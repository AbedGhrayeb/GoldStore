using Domain.Tenants;

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

    /// <summary>The current tenant lifecycle status. Throws when <see cref="IsAvailable"/> is false.</summary>
    TenantStatus Status { get; }

    /// <summary>
    /// True when the current tenant may reach operational endpoints: the tenant is
    /// <see cref="TenantStatus.Active"/>, a trial within its window, or a cancelled
    /// tenant inside its read-only grace period.
    /// </summary>
    bool IsOperational { get; }

    /// <summary>The features enabled for the current tenant. Throws when <see cref="IsAvailable"/> is false.</summary>
    IReadOnlyList<string> EnabledFeatures { get; }
}
