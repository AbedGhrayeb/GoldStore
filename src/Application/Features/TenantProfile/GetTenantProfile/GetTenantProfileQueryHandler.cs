using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenancy;
using Domain.Common;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.TenantProfile.GetTenantProfile;

internal sealed class GetTenantProfileQueryHandler(IPlatformDbContext platform, ITenantContext tenantContext)
    : IQueryHandler<GetTenantProfileQuery, TenantProfileResponse>
{
    public async Task<Result<TenantProfileResponse>> Handle(GetTenantProfileQuery query, CancellationToken cancellationToken)
    {
        Tenant? tenant = await platform.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == tenantContext.TenantId, cancellationToken);

        if (tenant is null)
        {
            return TenantErrors.NotFound(tenantContext.TenantId);
        }

        Plan? plan = await platform.Plans
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == tenant.PlanId, cancellationToken);

        if (plan is null)
        {
            return PlanErrors.NotFound(tenant.PlanId);
        }

        Subscription? subscription = await platform.Subscriptions
            .AsNoTracking()
            .Where(s => s.TenantId == tenant.Id && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new TenantProfileResponse(
            tenant.Id,
            tenant.Name,
            tenant.Subdomain,
            tenant.Status.ToString(),
            tenantContext.IsReadOnly,
            plan.Id,
            plan.Name,
            plan.Currency.ToCurrencyString(),
            tenant.SettingsJson,
            subscription?.Interval.ToString(),
            subscription?.Price,
            subscription?.StartsAtUtc,
            subscription?.ExpiresAtUtc,
            tenant.TrialEndsAtUtc,
            tenant.SubscriptionExpiresAtUtc,
            tenant.CreatedAtUtc);
    }
}
