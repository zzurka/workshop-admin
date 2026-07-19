namespace WorkshopAdmin.Application.Common.Codebooks;

/// <summary>
/// Identifies a table in the <c>codebook</c> schema. Acts as a compile-time
/// whitelist: callers cannot pass an arbitrary table name. Add members as
/// features need them; the name→table mapping (the actual SQL whitelist) lives
/// in the codebook repository implementation.
/// </summary>
public enum CodebookTable
{
    Currencies
}
