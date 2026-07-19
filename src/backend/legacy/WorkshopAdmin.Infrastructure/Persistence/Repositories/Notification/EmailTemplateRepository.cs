namespace WorkshopAdmin.Infrastructure.Persistence.Repositories.Notification;

using Dapper;
using System.Data;
using WorkshopAdmin.Application.Common.Models;
using WorkshopAdmin.Application.Common.Persistence;

public sealed class EmailTemplateRepository : IEmailTemplateRepository
{
    public Task<EmailTemplate?> GetByCodeAsync(
        string code,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, code, subject, body_text AS BodyText, body_html AS BodyHtml
            FROM notification.email_templates
            WHERE code = @Code
              AND is_active = TRUE
              AND is_deleted = FALSE
            """;

        return connection.QuerySingleOrDefaultAsync<EmailTemplate?>(new CommandDefinition(
            sql,
            new { Code = code },
            transaction,
            cancellationToken: cancellationToken));
    }
}
