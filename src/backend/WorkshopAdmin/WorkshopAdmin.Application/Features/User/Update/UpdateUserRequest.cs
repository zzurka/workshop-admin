namespace WorkshopAdmin.Application.Features.User.Update;

/// <summary>Edits a user's profile. Email, password, roles, and activation are changed via dedicated endpoints.</summary>
public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber);
