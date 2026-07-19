namespace WorkshopAdmin.Infrastructure.Email;

public sealed class EmailOptions
{
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;

    /// <summary>Locale used when an enqueue request omits one (e.g. "en").</summary>
    public string DefaultLocale { get; init; } = "en";

    /// <summary>How often the dispatcher polls the outbox.</summary>
    public int PollIntervalSeconds { get; init; } = 10;

    /// <summary>Max rows the dispatcher claims per poll.</summary>
    public int BatchSize { get; init; } = 25;

    public EmailSmtpOptions Smtp { get; init; } = new();
}

public sealed class EmailSmtpOptions
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public bool UseStartTls { get; init; } = true;
    public string? Username { get; init; }
    public string? Password { get; init; }
}
