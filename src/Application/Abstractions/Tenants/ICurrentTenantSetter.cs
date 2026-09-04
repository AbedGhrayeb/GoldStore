namespace Application.Abstractions.Tenants;

/// <summary>
/// Explicitly selects the ambient tenant for the current scope. Reserved for
/// non-request flows (database seeding, host administration, background jobs);
/// HTTP requests resolve the tenant from the authenticated claims instead.
/// </summary>
public interface ICurrentTenantSetter
{
    void Set(Guid tenantId, string tenantKey);
}
