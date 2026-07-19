using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using WorkshopAdmin.SharedKernel.Database;

namespace WorkshopAdmin.SharedKernel.Persistence;

/// <summary>
/// Base for per-module DbContexts. Attaches to the request's <see cref="IDbSession"/>
/// transaction (same connection, same RLS context as Dapper) and applies the shared
/// conventions: DB-generated defaults and soft-delete query filters. The EF model is
/// maintained by hand against the SQL schema — no EF migrations (DB-first).
/// </summary>
public abstract class ModuleDbContext : DbContext
{
    protected ModuleDbContext(DbContextOptions options, IDbSession session)
        : base(options)
    {
        if (session.Transaction is not null)
        {
            Database.UseTransaction(session.Transaction);
        }
    }

    /// <summary>Map this module's entities. Runs before the shared conventions.</summary>
    protected abstract void ConfigureModel(ModelBuilder modelBuilder);

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureModel(modelBuilder);
        ApplySharedConventions(modelBuilder);
    }

    private static void ApplySharedConventions(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.BaseType is not null || entityType.IsOwned())
            {
                continue;
            }

            Type clrType = entityType.ClrType;

            if (typeof(Entity).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType).Property(nameof(Entity.Id))
                    .HasDefaultValueSql("uuidv7()");
            }

            if (typeof(AuditableEntity).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType).Property(nameof(AuditableEntity.CreatedAt))
                    .HasDefaultValueSql("NOW()");

                // Soft delete: e => !e.IsDeleted
                ParameterExpression parameter = Expression.Parameter(clrType, "e");
                LambdaExpression filter = Expression.Lambda(
                    Expression.Not(Expression.Property(parameter, nameof(AuditableEntity.IsDeleted))),
                    parameter);
                modelBuilder.Entity(clrType).HasQueryFilter(filter);
            }
            else if (typeof(AppendOnlyEntity).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType).Property(nameof(AppendOnlyEntity.CreatedAt))
                    .HasDefaultValueSql("NOW()");
            }
        }
    }
}
