using System.Security.Claims;
using Application.Abstractions.Tenants;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Tenants;

/// <summary>
/// Scoped ambient tenant. On HTTP requests it is resolved from the authenticated
/// cookie claims (<c>tenant_id</c>/<c>tenant_key</c>); host and background flows
/// select the tenant explicitly through <see cref="Set"/>. It is never stored in
/// a singleton or static field, so tenants can never leak across requests.
/// </summary>
internal sealed class CurrentTenant(IHttpContextAccessor httpContextAccessor) : ICurrentTenant, ICurrentTenantSetter
{
    private Guid? _explicitTenantId;
    private string? _explicitTenantKey;

    public bool IsAvailable => Resolve() is not null;

    public Guid TenantId => Resolve()?.TenantId ?? throw new CurrentTenantUnavailableException();

    public string TenantKey => Resolve()?.TenantKey ?? throw new CurrentTenantUnavailableException();

    public void Set(Guid tenantId, string tenantKey)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id cannot be empty.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(tenantKey))
        {
            throw new ArgumentException("Tenant key cannot be empty.", nameof(tenantKey));
        }

        _explicitTenantId = tenantId;
        _explicitTenantKey = tenantKey;
    }

    private (Guid TenantId, string TenantKey)? Resolve()
    {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            string? tenantId = user.FindFirstValue(CustomClaims.TenantId);
            string? tenantKey = user.FindFirstValue(CustomClaims.TenantKey);

            if (Guid.TryParse(tenantId, out Guid parsedTenantId) && parsedTenantId != Guid.Empty
                && !string.IsNullOrWhiteSpace(tenantKey))
            {
                return (parsedTenantId, tenantKey);
            }
        }

        if (_explicitTenantId is { } explicitId && _explicitTenantKey is { } explicitKey)
        {
            return (explicitId, explicitKey);
        }

        return null;
    }
}
