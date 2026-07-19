namespace WorkshopAdmin.Application.Features.User.GetById;

public sealed record UserDetailResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<string> Roles);
