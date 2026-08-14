namespace Application.Abstractions.Caching;

/// <summary>
/// Key/tag conventions for tenant-scoped application caches (M7-C1). Every KPI/balance
/// entry is keyed and tagged per tenant so the single-tenant store can evict a tenant's
/// entries without cross-tenant key collisions or over-broad clears.
/// </summary>
public static class CacheKeys
{
    public const string KpiPrefix = "kpi";

    /// <summary>Safety net TTL; correctness comes from write-driven eviction.</summary>
    public static readonly TimeSpan KpiExpiration = TimeSpan.FromMinutes(5);

    public static string KpiTenant(Guid tenantId) => $"{KpiPrefix}:{tenantId}";

    public static string Kpi(Guid tenantId, string name) => $"{KpiPrefix}:{tenantId}:{name}";
}
