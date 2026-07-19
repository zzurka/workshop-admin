namespace WorkshopAdmin.Application.Features.Auth.Models;

/// <summary>
/// Identity claims extracted from a validated provider id_token.
/// </summary>
public sealed record ExternalIdentity(
    string Provider,
    string Subject,
    string Email,
    bool EmailVerified,
    string? FirstName,
    string? LastName);
