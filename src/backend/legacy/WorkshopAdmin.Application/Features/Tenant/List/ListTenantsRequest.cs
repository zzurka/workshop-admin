namespace WorkshopAdmin.Application.Features.Tenant.List;

using WorkshopAdmin.Application.Common.Models;

/// <summary>Query/filter for the tenant list. Inherits paging + sort from <see cref="PagedRequest"/>.</summary>
public sealed class ListTenantsRequest : PagedRequest
{
    /// <summary>Case-insensitive match against name, slug, or contact email.</summary>
    public string? Search { get; set; }

    /// <summary>Filter by activation state. Null = both.</summary>
    public bool? IsActive { get; set; }
}
