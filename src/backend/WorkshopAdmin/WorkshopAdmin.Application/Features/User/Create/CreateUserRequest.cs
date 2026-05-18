namespace WorkshopAdmin.Application.Features.User.Create;

/// <summary>
/// Creates a user in the caller's tenant. <see cref="RoleIds"/> is optional —
/// when supplied, every role must be assignable by the caller (tenant-scoped
/// global or own-tenant custom roles); otherwise the user is created with no
/// roles and can be granted them later.
/// </summary>
public sealed record CreateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string Password,
    IReadOnlyList<Guid>? RoleIds);
