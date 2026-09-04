using Application.Abstractions.Tenancy;
using Domain.Tenants;
using Infrastructure.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Tenancy;

internal sealed class TenantResolver(PlatformDbContext context, IMemoryCache cache) : ITenantResolver
{
    // Absolute (not sliding) expiration: tenant status changes (suspend/expire/provision)
    // must take effect within minutes without needing a cache invalidation channel.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<TenantInfo?> ResolveAsync(string subdomain, CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(CacheKey(subdomain), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            Tenant? tenant = await context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Subdomain == subdomain && t.IsActive, cancellationToken);

            return tenant is null
                ? null
                : new TenantInfo(
                    tenant.Id,
                    tenant.Subdomain,
                    tenant.SchemaName,
                    tenant.Status,
                    tenant.ConnectionString,
                    tenant.TrialEndsAtUtc,
                    tenant.SubscriptionExpiresAtUtc);
        });
    }

    public void Invalidate(string subdomain)
    {
        cache.Remove(CacheKey(subdomain));
    }

    private static string CacheKey(string subdomain) => $"tenant:subdomain:{subdomain}";
}
