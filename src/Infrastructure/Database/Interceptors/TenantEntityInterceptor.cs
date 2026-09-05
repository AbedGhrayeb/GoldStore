// <copyright file="TenantEntityInterceptor.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Tenants;
using Infrastructure.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SharedKernel;

namespace Infrastructure.Database.Interceptors;

/// <summary>
/// Central tenant write guard (plan Phase 3). Every write to a tenant-owned entity is
/// stamped and validated here, in exactly one place:
///  - Added rows are stamped with the current tenant when empty, and rejected when
///    they carry a foreign tenant or when no current tenant exists.
///  - TenantId is immutable: altering it on an existing row is rejected.
///  - Modified/deleted rows must belong to the current tenant (defense in depth on
///    top of the global query filter).
/// </summary>
public sealed class TenantEntityInterceptor(ICurrentTenant currentTenant) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        this.EnforceTenantRules(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        this.EnforceTenantRules(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void EnforceTenantRules(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (EntityEntry<ITenantEntity> entry in context.ChangeTracker.Entries<ITenantEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    this.StampOrValidateNewEntity(entry);
                    break;

                case EntityState.Modified:
                    this.GuardExistingEntity(entry);
                    break;

                case EntityState.Deleted:
                    this.GuardOwnedByCurrentTenant(entry.Entity);
                    break;
            }
        }
    }

    private void StampOrValidateNewEntity(EntityEntry<ITenantEntity> entry)
    {
        if (!currentTenant.IsAvailable)
        {
            throw new TenantAccessViolationException(
                $"Cannot create {entry.Entity.GetType().Name}: no current tenant is available. " +
                "Host and background flows must select a tenant explicitly before writing tenant-owned data.");
        }

        if (entry.Entity.TenantId == Guid.Empty)
        {
            entry.Property(nameof(ITenantEntity.TenantId)).CurrentValue = currentTenant.TenantId;
            return;
        }

        if (entry.Entity.TenantId != currentTenant.TenantId)
        {
            throw new TenantAccessViolationException(
                $"Cannot create {entry.Entity.GetType().Name} for tenant {entry.Entity.TenantId}: " +
                $"the current tenant is {currentTenant.TenantId}. Client-supplied tenant ids are never accepted.");
        }
    }

    private void GuardExistingEntity(EntityEntry<ITenantEntity> entry)
    {
        PropertyEntry entryProperty = entry.Property(nameof(ITenantEntity.TenantId));
        if (entryProperty.IsModified && !Equals(entryProperty.OriginalValue, entryProperty.CurrentValue))
        {
            throw new TenantAccessViolationException(
                $"Cannot change the tenant of {entry.Entity.GetType().Name}: TenantId is immutable.");
        }

        this.GuardOwnedByCurrentTenant(entry.Entity);
    }

    private void GuardOwnedByCurrentTenant(ITenantEntity entity)
    {
        if (currentTenant.IsAvailable && entity.TenantId != currentTenant.TenantId)
        {
            throw new TenantAccessViolationException(
                $"Cannot modify {entity.GetType().Name}: it belongs to a different tenant.");
        }
    }
}
