namespace WorkshopAdmin.Domain.Entities.Codebook;

public class CodebookEntry
{
    public short Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Dictionary<string, string> Label { get; set; } = new();
    public short SortOrder { get; set; }
    public bool IsActive { get; set; }
}
