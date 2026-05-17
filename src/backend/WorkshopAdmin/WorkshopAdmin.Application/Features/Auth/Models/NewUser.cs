namespace WorkshopAdmin.Application.Features.Auth.Models;

/// <summary>Data required to create an auth.users row.</summary>
public sealed record NewUser(
    string Email,
    string PasswordHash,
    string FirstName,
    string LastName,
    Guid? TenantId);
