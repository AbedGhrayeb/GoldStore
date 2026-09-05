// <copyright file="GetSubscriptionPlansQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.SubscriptionPlans;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.SubscriptionPlans.GetSubscriptionPlans;

internal sealed class GetSubscriptionPlansQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetSubscriptionPlansQuery, IReadOnlyList<SubscriptionPlanResponse>>
{
    public async Task<Result<IReadOnlyList<SubscriptionPlanResponse>>> Handle(
        GetSubscriptionPlansQuery query,
        CancellationToken cancellationToken)
    {
        List<SubscriptionPlanResponse> plans = await context.SubscriptionPlans
            .AsNoTracking()
            .OrderBy(plan => plan.Name)
            .Select(plan => new SubscriptionPlanResponse(
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
                plan.DiscountPercent == null ? plan.Price : Math.Round(plan.Price * (1 - (plan.DiscountPercent.Value / 100m)), 2)))
            .ToListAsync(cancellationToken);

        return plans;
    }
}
