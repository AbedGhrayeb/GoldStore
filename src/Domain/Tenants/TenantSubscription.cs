using SharedKernel;
using SharedKernel.Result;

namespace Domain.Tenants;

public sealed class TenantSubscription : Entity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public Guid SubscriptionPlanId { get; private set; }

    public SubscriptionStatus Status { get; private set; }

    public SubscriptionBillingCycle BillingCycle { get; private set; }

    public DateTimeOffset StartsAtUtc { get; private set; }

    public DateTimeOffset EndsAtUtc { get; private set; }

    public string? BillingProvider { get; private set; }

    public string? BillingProviderReference { get; private set; }

    private TenantSubscription()
    {
    }

    private TenantSubscription(Guid id, Guid tenantId, Guid subscriptionPlanId, SubscriptionBillingCycle billingCycle,
        DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, string? billingProvider, string? billingProviderReference) : base(id)
    {
        TenantId = tenantId;
        SubscriptionPlanId = subscriptionPlanId;
        Status = SubscriptionStatus.Active;
        BillingCycle = billingCycle;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        BillingProvider = billingProvider;
        BillingProviderReference = billingProviderReference;
    }

    public static Result<TenantSubscription> Create(Guid tenantId, Guid subscriptionPlanId, SubscriptionBillingCycle billingCycle,
        DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, string? billingProvider = null, string? billingProviderReference = null)
    {
        if (tenantId == Guid.Empty || subscriptionPlanId == Guid.Empty)
        {
            return TenantErrors.IdRequired;
        }

        if (endsAtUtc <= startsAtUtc)
        {
            return TenantErrors.InvalidSubscriptionPeriod;
        }

        return new TenantSubscription(Guid.CreateVersion7(), tenantId, subscriptionPlanId, billingCycle,
            startsAtUtc, endsAtUtc, billingProvider?.Trim(), billingProviderReference?.Trim());
    }

    public Result<Updated> Renew(DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc)
    {
        if (endsAtUtc <= startsAtUtc)
        {
            return TenantErrors.InvalidSubscriptionPeriod;
        }

        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        Status = SubscriptionStatus.Active;

        return Result.Updated;
    }

    public void MarkPastDue()
    {
        Status = SubscriptionStatus.PastDue;
    }

    public void Cancel()
    {
        Status = SubscriptionStatus.Cancelled;
    }

    public void Expire()
    {
        Status = SubscriptionStatus.Expired;
    }
}
