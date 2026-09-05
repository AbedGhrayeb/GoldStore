// <copyright file="CurrentTenant.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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
    private Guid? explicitTenantId;
    private string? explicitTenantKey;
    private TenantSnapshot? tenantSnapshot;
    private IReadOnlyList<string>? enabledFeatures;

    public bool IsAvailable => this.Resolve() is not null;

    public Guid TenantId => this.Resolve()?.TenantId ?? throw new CurrentTenantUnavailableException();

    public string TenantKey => this.Resolve()?.TenantKey ?? throw new CurrentTenantUnavailableException();

    public TenantStatus Status => this.ResolveTenant()?.Status ?? throw new CurrentTenantUnavailableException();

    public bool IsOperational => this.IsAvailable && this.IsOperationalInternal();

    public bool IsReadOnly => this.IsAvailable && this.Status == TenantStatus.Cancelled;

    public IReadOnlyList<string> EnabledFeatures => this.ResolveEnabledFeatures();

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

        this.explicitTenantId = tenantId;
        this.explicitTenantKey = tenantKey;
    }

    private bool IsOperationalInternal()
    {
        TenantSnapshot? snapshot = this.ResolveTenant();
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
        if (this.tenantSnapshot is not null)
        {
            return this.tenantSnapshot;
        }

        (Guid TenantId, string TenantKey)? resolved = this.Resolve();
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

        this.tenantSnapshot = tenant is null
            ? null
            : new TenantSnapshot(tenant.Status, tenant.TrialEndsAtUtc, tenant.CancellationReadOnlyUntilUtc);

        return this.tenantSnapshot;
    }

    private IReadOnlyList<string> ResolveEnabledFeatures()
    {
        if (this.enabledFeatures is not null)
        {
            return this.enabledFeatures;
        }

        (Guid TenantId, string TenantKey)? resolved = this.Resolve() ?? throw new CurrentTenantUnavailableException();

        IApplicationDbContext context = serviceProvider.GetRequiredService<IApplicationDbContext>();

        this.enabledFeatures = context.TenantSettings
            .AsNoTracking()
            .Where(settings => settings.TenantId == resolved.Value.TenantId)
            .Select(settings => settings.EnabledFeatures)
            .SingleOrDefault() ?? [];

        return this.enabledFeatures;
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

        if (this.explicitTenantId is { } explicitId && this.explicitTenantKey is { } explicitKey)
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
