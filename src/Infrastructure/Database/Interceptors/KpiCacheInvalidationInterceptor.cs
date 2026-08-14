using Application.Abstractions.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SharedKernel;

namespace Infrastructure.Database.Interceptors;

/// <summary>
/// M7-C1: evicts a tenant's KPI/balance cache entries whenever any of its tenant-owned
/// rows are written (added, modified, deleted). Balances and KPIs are aggregates over
/// ledger entries and are never stored directly, so any mutation can change them; the
/// next read recomputes from the database. Tag eviction is the cache-correctness backstop
/// and the per-entry TTL bounds staleness if a write path is ever missed.
/// </summary>
public sealed class KpiCacheInvalidationInterceptor(ICacheService cache) : SaveChangesInterceptor
{
    private readonly HashSet<Guid> _affectedTenants = new();

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        CollectAffectedTenants(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (_affectedTenants.Count > 0)
        {
            await EvictAsync(cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void CollectAffectedTenants(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (EntityEntry<ITenantEntity> entry in context.ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                _affectedTenants.Add(entry.Entity.TenantId);
            }
        }
    }

    private async Task EvictAsync(CancellationToken cancellationToken)
    {
        foreach (Guid tenantId in _affectedTenants)
        {
            await cache.RemoveByTagAsync(CacheKeys.KpiTenant(tenantId), cancellationToken);
        }

        _affectedTenants.Clear();
    }
}
