using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Platform.Tenants.GetTenants;

internal sealed class GetTenantsQueryHandler(IPlatformDbContext platform)
    : IQueryHandler<GetTenantsQuery, List<TenantSummaryResponse>>
{
    public async Task<Result<List<TenantSummaryResponse>>> Handle(GetTenantsQuery query, CancellationToken cancellationToken)
    {
        var rows = await platform.Tenants
            .AsNoTracking()
            .Join(
                platform.Plans,
                tenant => tenant.PlanId,
                plan => plan.Id,
                (tenant, plan) => new { tenant, plan })
            .OrderByDescending(x => x.tenant.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new TenantSummaryResponse(
                x.tenant.Id,
                x.tenant.Name,
                x.tenant.Subdomain,
                x.tenant.SchemaName,
                x.tenant.Status.ToString(),
                x.plan.Name,
                x.tenant.CreatedAtUtc,
                x.tenant.TrialEndsAtUtc,
                x.tenant.SubscriptionExpiresAtUtc))
            .ToList();
    }
}
