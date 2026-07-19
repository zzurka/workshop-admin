namespace WorkshopAdmin.Application.Features.User.List;

public sealed record UserListItem(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsActive,
    DateTime CreatedAt);
