// <copyright file="GetTenantSubscriptionQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.Subscriptions;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Subscriptions.GetTenantSubscription;

internal sealed class GetTenantSubscriptionQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetTenantSubscriptionQuery, TenantSubscriptionResponse>
{
    public async Task<Result<TenantSubscriptionResponse>> Handle(
        GetTenantSubscriptionQuery query,
        CancellationToken cancellationToken)
    {
        Tenant? tenant = await context.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == query.TenantId, cancellationToken);

        if (tenant is null)
        {
            return TenantErrors.NotFound(query.TenantId);
        }

        TenantSubscription? subscription = await context.TenantSubscriptions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.TenantId == query.TenantId)
            .OrderByDescending(s => s.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
        {
            return Error.NotFound("Tenants.Subscription.NotFound", $"لا يوجد اشتراك للمتجر بـ Id = '{query.TenantId}'");
        }

        SubscriptionPlan? plan = await context.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == subscription.SubscriptionPlanId, cancellationToken);

        if (plan is null)
        {
            return TenantErrors.PlanNotFound(subscription.SubscriptionPlanId);
        }

        TenantSubscriptionResponse response = new(
            subscription.Id,
            subscription.TenantId,
            subscription.Status.ToString(),
            subscription.BillingCycle.ToString(),
            subscription.StartsAtUtc,
            subscription.EndsAtUtc,
            subscription.BillingProvider,
            subscription.BillingProviderReference,
            new SubscriptionPlanSnapshot(
                plan.Id,
                plan.Name,
                plan.Key,
                plan.MaximumActiveUsers,
                plan.MaximumPostedInvoicesPerPeriod,
                plan.MaximumActiveBranches,
                plan.MaximumStorageBytes,
                plan.IsActive,
                plan.IsTrial,
                plan.DurationInMonths,
                plan.Price,
                plan.DiscountPercent,
                plan.EffectivePrice));

        return response;
    }
}
