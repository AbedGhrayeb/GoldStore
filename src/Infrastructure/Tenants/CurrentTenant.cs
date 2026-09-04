using System.Security.Claims;
using Application.Abstractions.Data;
using Application.Abstractions.Tenants;
using Domain.Tenants;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tenants;

/// <summary>
/// Scoped ambient tenant. On HTTP requests it is resolved from the authenticated
/// cookie claims (<c>tenant_id</c>/<c>tenant_key</c>); host and background flows
/// select the tenant explicitly through <see cref="Set"/>. It is never stored in
/// a singleton or static field, so tenants can never leak across requests.
/// Status and enabled features are loaded from the database lazily on first access
/// and cached for the scope.
/// </summary>
internal sealed class CurrentTenant(
    IHttpContextAccessor httpContextAccessor,
    IServiceProvider serviceProvider,
    TimeProvider timeProvider) : ICurrentTenant, ICurrentTenantSetter
{
    private Guid? _explicitTenantId;
    private string? _explicitTenantKey;
    private TenantSnapshot? _tenantSnapshot;
    private IReadOnlyList<string>? _enabledFeatures;

    public bool IsAvailable => Resolve() is not null;

    public Guid TenantId => Resolve()?.TenantId ?? throw new CurrentTenantUnavailableException();

    public string TenantKey => Resolve()?.TenantKey ?? throw new CurrentTenantUnavailableException();

    public TenantStatus Status => ResolveTenant()?.Status ?? throw new CurrentTenantUnavailableException();

    public bool IsOperational => IsAvailable && IsOperationalInternal();

    public bool IsReadOnly => IsAvailable && Status == TenantStatus.Cancelled;

    public IReadOnlyList<string> EnabledFeatures => ResolveEnabledFeatures();

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

    private bool IsOperationalInternal()
    {
        TenantSnapshot? snapshot = ResolveTenant();
        if (snapshot is null)
        {
            return false;
        }

        DateTimeOffset utcNow = timeProvider.GetUtcNow();

        return snapshot.Status switch
        {
            TenantStatus.Active => true,
            TenantStatus.Trial => snapshot.TrialEndsAtUtc is null || snapshot.TrialEndsAtUtc > utcNow,
            TenantStatus.Cancelled => snapshot.CancellationReadOnlyUntilUtc is not null
                && snapshot.CancellationReadOnlyUntilUtc > utcNow,
            _ => false,
        };
    }

    private TenantSnapshot? ResolveTenant()
    {
        if (_tenantSnapshot is not null)
        {
            return _tenantSnapshot;
        }

        (Guid TenantId, string TenantKey)? resolved = Resolve();
        if (resolved is null)
        {
            return null;
        }

        // Resolved lazily through the service provider to avoid a construction-time
        // circular dependency between CurrentTenant and the DbContext.
        IApplicationDbContext context = serviceProvider.GetRequiredService<IApplicationDbContext>();

        Tenant? tenant = context.Tenants
            .AsNoTracking()
            .SingleOrDefault(entity => entity.Id == resolved.Value.TenantId);

        _tenantSnapshot = tenant is null
            ? null
            : new TenantSnapshot(tenant.Status, tenant.TrialEndsAtUtc, tenant.CancellationReadOnlyUntilUtc);

        return _tenantSnapshot;
    }

    private IReadOnlyList<string> ResolveEnabledFeatures()
    {
        if (_enabledFeatures is not null)
        {
            return _enabledFeatures;
        }

        (Guid TenantId, string TenantKey)? resolved = Resolve() ?? throw new CurrentTenantUnavailableException();

        IApplicationDbContext context = serviceProvider.GetRequiredService<IApplicationDbContext>();

        _enabledFeatures = context.TenantSettings
            .AsNoTracking()
            .Where(settings => settings.TenantId == resolved.Value.TenantId)
            .Select(settings => settings.EnabledFeatures)
            .SingleOrDefault() ?? [];

        return _enabledFeatures;
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

    private sealed record TenantSnapshot(
        TenantStatus Status,
        DateTimeOffset? TrialEndsAtUtc,
        DateTimeOffset? CancellationReadOnlyUntilUtc);
}
