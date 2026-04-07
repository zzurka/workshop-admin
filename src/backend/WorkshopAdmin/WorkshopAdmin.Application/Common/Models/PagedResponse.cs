namespace WorkshopAdmin.Application.Common.Models;

public class PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; }
}
