using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Subscriptions.Renew;

internal sealed class RenewTenantSubscriptionCommandHandler(IApplicationDbContext context)
    : ICommandHandler<RenewTenantSubscriptionCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RenewTenantSubscriptionCommand command, CancellationToken cancellationToken)
    {
        if (command.EndsAtUtc <= command.StartsAtUtc)
        {
            return TenantErrors.InvalidSubscriptionPeriod;
        }

        Tenant? tenant = await context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == command.TenantId, cancellationToken);

        if (tenant is null)
        {
            return TenantErrors.NotFound(command.TenantId);
        }

        TenantSubscription? subscription = await context.TenantSubscriptions
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == command.TenantId)
            .OrderByDescending(s => s.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        Guid targetPlanId;
        SubscriptionPlan? resolvedPlan = null;

        if (command.NewPlanId.HasValue)
        {
            targetPlanId = command.NewPlanId.Value;
            resolvedPlan = await context.SubscriptionPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == targetPlanId, cancellationToken);
            if (resolvedPlan is null)
            {
                return TenantErrors.PlanNotFound(targetPlanId);
            }

            if (!resolvedPlan.IsActive)
            {
                return Error.Validation("Tenants.Plan.NotActive", "خطة الاشتراك غير نشطة");
            }

            if (resolvedPlan.IsTrial)
            {
                return TenantErrors.TrialCannotRenew;
            }
        }
        else if (subscription is not null)
        {
            targetPlanId = subscription.SubscriptionPlanId;
            resolvedPlan = await context.SubscriptionPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == targetPlanId, cancellationToken);
            if (resolvedPlan is not null && resolvedPlan.IsTrial)
            {
                return TenantErrors.TrialCannotRenew;
            }
        }
        else
        {
            return TenantErrors.PlanNotFound(Guid.Empty);
        }

        if (subscription is null)
        {
            Result<TenantSubscription> createResult = TenantSubscription.Create(
                command.TenantId,
                targetPlanId,
                command.BillingCycle,
                command.StartsAtUtc,
                command.EndsAtUtc);

            if (createResult.IsError)
            {
                return createResult.Errors;
            }

            context.TenantSubscriptions.Add(createResult.Value);
            await context.SaveChangesAsync(cancellationToken);
            return createResult.Value.Id;
        }

        // Plan change if needed.
        if (subscription.SubscriptionPlanId != targetPlanId)
        {
            Result<Updated> changePlan = subscription.ChangePlan(targetPlanId);
            if (changePlan.IsError)
            {
                return changePlan.Errors;
            }
        }

        subscription.ChangeBillingCycle(command.BillingCycle);

        Result<Updated> renewResult = subscription.Renew(command.StartsAtUtc, command.EndsAtUtc);
        if (renewResult.IsError)
        {
            return renewResult.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);
        return subscription.Id;
    }
}
