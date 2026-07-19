namespace WorkshopAdmin.Infrastructure.Email;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data.Common;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Common.Models;
using WorkshopAdmin.Application.Common.Persistence;

/// <summary>
/// Background service that drains <c>notification.email_outbox</c>: claims due
/// rows, sends them via <see cref="ISmtpClient"/>, and marks each row sent or
/// scheduled-for-retry. Exponential backoff on failure; <c>FOR UPDATE SKIP
/// LOCKED</c> in the claim query makes this safe to run on multiple instances.
/// </summary>
public sealed class EmailDispatcherHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOptions> emailOptions,
    ILogger<EmailDispatcherHostedService> logger) : BackgroundService
{
    private readonly EmailOptions _options = emailOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan pollInterval = TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Email dispatcher tick failed.");
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task DispatchOnceAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        IDbConnectionFactory connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        IEmailOutboxRepository repository = scope.ServiceProvider.GetRequiredService<IEmailOutboxRepository>();
        ISmtpClient smtpClient = scope.ServiceProvider.GetRequiredService<ISmtpClient>();

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        IReadOnlyList<EmailOutboxRecord> batch = await repository.ClaimBatchAsync(
            _options.BatchSize, connection, cancellationToken);

        foreach (EmailOutboxRecord row in batch)
        {
            try
            {
                await smtpClient.SendAsync(
                    row.ToAddress, row.ToName, row.Subject, row.BodyText, row.BodyHtml, cancellationToken);

                await repository.MarkSentAsync(row.Id, connection, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                bool permanent = row.Attempts >= row.MaxAttempts;
                DateTime? nextAttempt = permanent ? null : DateTime.UtcNow.Add(Backoff(row.Attempts));

                logger.LogWarning(
                    ex,
                    "Email outbox row {Id} send failed (attempt {Attempt}/{Max}). {Outcome}",
                    row.Id, row.Attempts, row.MaxAttempts,
                    permanent ? "Marking failed." : $"Retrying at {nextAttempt:O}.");

                await repository.MarkFailureAsync(row.Id, ex.Message, nextAttempt, connection, cancellationToken);
            }
        }
    }

    private static TimeSpan Backoff(int attempts)
    {
        // attempts is already incremented by the claim query, so attempts=1 on
        // first failure => ~2 min, capped at 1h.
        double minutes = Math.Min(60d, Math.Pow(2, Math.Max(0, attempts)));
        return TimeSpan.FromMinutes(minutes);
    }
}
