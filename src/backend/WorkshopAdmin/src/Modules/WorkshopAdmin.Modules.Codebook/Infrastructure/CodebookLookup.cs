using WorkshopAdmin.Modules.Codebook.Contracts;
using WorkshopAdmin.Modules.Codebook.Persistence;

namespace WorkshopAdmin.Modules.Codebook.Infrastructure;

internal sealed class CodebookLookup(CodebookRegistry registry, CodebookCache cache, CodebookDbContext db)
    : ICodebookLookup
{
    public async Task<short?> GetIdByCodeAsync(string type, string code, CancellationToken cancellationToken = default)
    {
        if (!registry.TryGet(type, out CodebookType codebookType))
        {
            return null;
        }

        IReadOnlyList<CodebookEntryItem> entries = await cache.GetOrLoadAsync(codebookType, db, cancellationToken);
        return entries.FirstOrDefault(e => e.Code == code)?.Id;
    }

    public async Task<CodebookEntryRef?> GetByIdAsync(string type, short id, CancellationToken cancellationToken = default)
    {
        if (!registry.TryGet(type, out CodebookType codebookType))
        {
            return null;
        }

        IReadOnlyList<CodebookEntryItem> entries = await cache.GetOrLoadAsync(codebookType, db, cancellationToken);
        CodebookEntryItem? entry = entries.FirstOrDefault(e => e.Id == id);
        return entry is null ? null : new CodebookEntryRef(entry.Id, entry.Code, entry.Label, entry.IsActive);
    }
}
