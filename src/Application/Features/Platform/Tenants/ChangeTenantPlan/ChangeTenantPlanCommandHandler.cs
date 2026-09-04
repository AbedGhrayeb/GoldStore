using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenancy;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Result;

namespace Application.Features.Platform.Tenants.ChangeTenantPlan;

internal sealed class ChangeTenantPlanCommandHandler(
    IPlatformDbContext platform,
    ITenantResolver tenantResolver,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ChangeTenantPlanCommand, Updated>
{
    public async Task<Result<Updated>> Handle(ChangeTenantPlanCommand command, CancellationToken cancellationToken)
    {
        Tenant? tenant = await platform.Tenants
            .FirstOrDefaultAsync(t => t.Id == command.TenantId, cancellationToken);

        if (tenant is null)
        {
            return TenantErrors.NotFound(command.TenantId);
        }

        Plan? plan = await platform.Plans
            .FirstOrDefaultAsync(p => p.Id == command.NewPlanId && p.IsActive, cancellationToken);

        if (plan is null)
        {
            return PlanErrors.NotFound(command.NewPlanId);
        }

        var now = new DateTimeOffset(dateTimeProvider.UtcNow, TimeSpan.Zero);
        DateTimeOffset expiryBase = tenant.SubscriptionExpiresAtUtc > now ? tenant.SubscriptionExpiresAtUtc.Value : now;
        DateTimeOffset expiresAt = command.Interval == BillingInterval.Annual ? expiryBase.AddYears(1) : expiryBase.AddMonths(1);
        decimal price = command.Interval == BillingInterval.Annual ? plan.AnnualPrice : plan.MonthlyPrice;

        await CancelActiveSubscriptionsAsync(tenant.Id, cancellationToken);

        Result<Subscription> subscriptionResult = Subscription.Create(
            tenant.Id, plan.Id, command.Interval, price, plan.Currency, now, expiresAt);

        if (subscriptionResult.IsError)
        {
            return subscriptionResult.Errors;
        }

        platform.Subscriptions.Add(subscriptionResult.Value);
        tenant.ChangePlan(plan.Id);
        tenant.RenewSubscription(expiresAt);

        await platform.SaveChangesAsync(cancellationToken);

        tenantResolver.Invalidate(tenant.Subdomain);

        return Result.Updated;
    }

    private async Task CancelActiveSubscriptionsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        List<Subscription> activeSubscriptions = await platform.Subscriptions
            .Where(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (Subscription subscription in activeSubscriptions)
        {
            subscription.Cancel();
        }
    }
}
