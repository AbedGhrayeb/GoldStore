using Application.Abstractions.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SharedKernel;

namespace Infrastructure.Database.Interceptors;

public class AuditableEntityInterceptor(IUserContext user, TimeProvider dateTime) : SaveChangesInterceptor
{
    private readonly IUserContext _user = user;
    private readonly TimeProvider _dateTime = dateTime;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public void UpdateEntities(DbContext? context)
    {
        if (context == null)
        {
            return;
        }

        // Host and background flows (seeding, provisioning, migrations) run without
        // an authenticated user; audit fields stay unset for those writes.
        if (!_user.IsAvailable)
        {
            return;
        }

        foreach (EntityEntry<AuditableEntity> entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                DateTimeOffset utcNow = _dateTime.GetUtcNow();

                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedBy = _user.UserId;
                    entry.Entity.CreatedAtUtc = utcNow;
                }

                entry.Entity.LastModifiedBy = _user.UserId;
                entry.Entity.LastModifiedUtc = utcNow;

                foreach (ReferenceEntry ownedEntry in entry.References)
                {
                    if (ownedEntry.TargetEntry is { Entity: AuditableEntity ownedEntity } && ownedEntry.TargetEntry.State is EntityState.Added or EntityState.Modified)
                    {
                        if (ownedEntry.TargetEntry.State == EntityState.Added)
                        {
                            ownedEntity.CreatedBy = _user.UserId;
                            ownedEntity.CreatedAtUtc = utcNow;
                        }

                        ownedEntity.LastModifiedBy = _user.UserId;
                        ownedEntity.LastModifiedUtc = utcNow;
                    }
                }
            }
        }
    }
}

public static class Extensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
        entry.References.Any(r =>
            r.TargetEntry?.Metadata.IsOwned() == true &&
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}
