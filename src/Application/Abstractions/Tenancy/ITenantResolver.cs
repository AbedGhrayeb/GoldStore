namespace Application.Abstractions.Tenancy;

public interface ITenantResolver
{
    Task<TenantInfo?> ResolveAsync(string subdomain, CancellationToken cancellationToken = default);

    /// <summary>Drops the cached tenant (call after status/plan changes so they take effect immediately).</summary>
    void Invalidate(string subdomain);
}
