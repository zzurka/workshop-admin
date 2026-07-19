using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkshopAdmin.SharedKernel.Database;
using WorkshopAdmin.SharedKernel.Persistence;

namespace WorkshopAdmin.Modules.Codebook.Persistence;

/// <summary>
/// Maps the <c>codebook</c> schema only. The table list comes from
/// <see cref="CodebookRegistry"/> — every registered type maps to its own table
/// (slug = table name), no inheritance hierarchy.
/// </summary>
internal sealed class CodebookDbContext(DbContextOptions<CodebookDbContext> options, IDbSession session)
    : ModuleDbContext(options, session)
{
    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        foreach (CodebookType type in new CodebookRegistry().All)
        {
            EntityTypeBuilder entity = modelBuilder.Entity(type.ClrType);
            entity.ToTable(type.Slug, "codebook");
            entity.HasKey(nameof(CodebookEntry.Id));
            entity.Property(nameof(CodebookEntry.Id)).ValueGeneratedOnAdd();
            entity.Property(nameof(CodebookEntry.Label))
                .HasColumnType("jsonb")
                .HasConversion(JsonbLabel.Converter, JsonbLabel.Comparer);
        }
    }
}
