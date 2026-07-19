using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WorkshopAdmin.SharedKernel.Auth;

namespace WorkshopAdmin.SharedKernel.Persistence;

/// <summary>
/// Fills audit columns from <see cref="ICurrentUser"/> on every SaveChanges — one place
/// instead of per-query discipline. Manual Dapper SQL must set them explicitly.
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (EntityEntry<AuditableEntity> entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy ??= currentUser.UserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                entry.Entity.UpdatedBy = currentUser.UserId;
            }
        }

        foreach (EntityEntry<AppendOnlyEntity> entry in context.ChangeTracker.Entries<AppendOnlyEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy ??= currentUser.UserId;
            }
        }
    }
}
