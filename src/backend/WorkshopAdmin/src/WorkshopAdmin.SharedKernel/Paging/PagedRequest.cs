namespace WorkshopAdmin.SharedKernel.Paging;

public class PagedRequest
{
    public int Offset { get; set; }
    public int Limit { get; set; } = 25;
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "asc";
}
