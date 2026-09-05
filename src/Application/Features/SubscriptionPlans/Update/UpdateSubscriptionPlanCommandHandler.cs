// <copyright file="UpdateSubscriptionPlanCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.SubscriptionPlans.Update;

internal sealed class UpdateSubscriptionPlanCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateSubscriptionPlanCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UpdateSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        SubscriptionPlan? plan = await context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (plan is null)
        {
            return TenantErrors.PlanNotFound(command.Id);
        }

        string normalizedKey = string.IsNullOrWhiteSpace(command.Key)
            ? SubscriptionPlan.GenerateKey(command.Name)
            : command.Key.Trim().ToLowerInvariant();

        bool keyExists = await context.SubscriptionPlans
            .AsNoTracking()
            .AnyAsync(p => p.Key == normalizedKey && p.Id != command.Id, cancellationToken);

        if (keyExists)
        {
            return TenantErrors.KeyNotUnique;
        }

        Result<Updated> updateResult = plan.Update(
            command.Name,
            normalizedKey,
            command.MaximumActiveUsers,
            command.MaximumPostedInvoicesPerPeriod,
            command.MaximumActiveBranches,
            command.MaximumStorageBytes,
            command.IsTrial,
            command.DurationInMonths,
            command.Price,
            command.DiscountPercent,
            command.IsActive);

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        return plan.Id;
    }
}
