namespace WorkshopAdmin.Application.Features.User.List;

using WorkshopAdmin.Application.Common.Models;

public sealed class ListUsersRequest : PagedRequest
{
    /// <summary>Case-insensitive match against email, first name, or last name.</summary>
    public string? Search { get; set; }

    /// <summary>Filter by activation state. Null = both.</summary>
    public bool? IsActive { get; set; }
}
