using Application.Abstractions.Data;
using Application.Abstractions.Subscriptions;
using Application.Abstractions.Tenants;
using Application.Common.Errors;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Infrastructure.Subscriptions;

/// <summary>
/// Reads the current tenant's subscription plan and enforces its limits (plan Phase 4
/// item 7). Limits that are null are unlimited; tenants without a subscription or plan
/// are treated as unlimited so a misconfiguration can never lock a store out.
/// </summary>
internal sealed class SubscriptionGate(
    IApplicationDbContext context,
    ICurrentTenant currentTenant) : ISubscriptionGate
{
    public async Task<Result<Success>> EnsureCanAddUsersAsync(int additionalUsers, CancellationToken cancellationToken)
    {
        if (!currentTenant.IsAvailable)
        {
            return ApplicationErrors.TenantAccessDenied;
        }

        PlanAccess plan = await LoadPlanAccessAsync(cancellationToken);

        if (plan.MaximumActiveUsers is not { } limit)
        {
            return Result.Success;
        }

        int currentUsers = await context.Users.CountAsync(user => user.IsActive, cancellationToken);

        return currentUsers + additionalUsers <= limit
            ? Result.Success
            : TenantErrors.QuotaExceeded("المستخدمين النشطين", limit);
    }

    public async Task<Result<Success>> EnsureCanPostInvoicesAsync(int additionalInvoices, CancellationToken cancellationToken)
    {
        if (!currentTenant.IsAvailable)
        {
            return ApplicationErrors.TenantAccessDenied;
        }

        PlanAccess plan = await LoadPlanAccessAsync(cancellationToken);

        if (plan.MaximumPostedInvoicesPerPeriod is not { } limit)
        {
            return Result.Success;
        }

        int currentInvoices = await context.SalesInvoices
            .CountAsync(invoice =>
                invoice.Date.Date >= plan.PeriodStartsAtUtc.UtcDateTime.Date
                && invoice.Date.Date <= plan.PeriodEndsAtUtc.UtcDateTime.Date,
                cancellationToken);

        return currentInvoices + additionalInvoices <= limit
            ? Result.Success
            : TenantErrors.QuotaExceeded("الفواتير", limit);
    }

    private async Task<PlanAccess> LoadPlanAccessAsync(CancellationToken cancellationToken)
    {
        // TenantSubscriptions is tenant-owned, so the global query filter scopes it to
        // the current tenant; SubscriptionPlans is global reference data.
        TenantSubscription? subscription = await context.TenantSubscriptions
            .AsNoTracking()
            .OrderByDescending(item => item.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
        {
            return PlanAccess.Unlimited;
        }

        SubscriptionPlan? plan = await context.SubscriptionPlans
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == subscription.SubscriptionPlanId, cancellationToken);

        if (plan is null)
        {
            return PlanAccess.Unlimited;
        }

        return new PlanAccess(
            subscription.StartsAtUtc,
            subscription.EndsAtUtc,
            plan.MaximumActiveUsers,
            plan.MaximumPostedInvoicesPerPeriod);
    }

    private sealed record PlanAccess(
        DateTimeOffset PeriodStartsAtUtc,
        DateTimeOffset PeriodEndsAtUtc,
        int? MaximumActiveUsers,
        int? MaximumPostedInvoicesPerPeriod)
    {
        public static readonly PlanAccess Unlimited = new(
            DateTimeOffset.MinValue, DateTimeOffset.MaxValue, null, null);
    }
}
