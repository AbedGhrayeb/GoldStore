namespace Application.Abstractions.Tenants;

/// <summary>
/// The ambient tenant for the current operation. For HTTP requests it is resolved
/// from the authenticated user's claims; for host and background flows it is set
/// explicitly through <see cref="ICurrentTenantSetter"/>. Never read the tenant
/// from request bodies, route values, or query strings.
/// </summary>
public interface ICurrentTenant
{
    /// <summary>True when a tenant has been resolved for the current scope.</summary>
    bool IsAvailable { get; }

    /// <summary>The current tenant id. Throws when <see cref="IsAvailable"/> is false.</summary>
    Guid TenantId { get; }

    /// <summary>The current tenant key. Throws when <see cref="IsAvailable"/> is false.</summary>
    string TenantKey { get; }
}
