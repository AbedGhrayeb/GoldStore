using Domain.Common;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Tenants;

public sealed class Subscription : Entity
{
    public Guid TenantId { get; private set; }
    public Guid PlanId { get; private set; }
    public BillingInterval Interval { get; private set; }
    public decimal Price { get; private set; }
    public Currency Currency { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public SubscriptionStatus Status { get; private set; }

    private Subscription() { }

    private Subscription(Guid id, Guid tenantId, Guid planId, BillingInterval interval, decimal price, Currency currency, DateTimeOffset startsAtUtc, DateTimeOffset expiresAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        PlanId = planId;
        Interval = interval;
        Price = price;
        Currency = currency;
        StartsAtUtc = startsAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Status = SubscriptionStatus.Active;
    }

    public static Result<Subscription> Create(Guid tenantId, Guid planId, BillingInterval interval, decimal price, Currency currency, DateTimeOffset startsAtUtc, DateTimeOffset expiresAtUtc)
    {
        if (tenantId == Guid.Empty)
        {
            return SubscriptionErrors.TenantRequired;
        }

        if (planId == Guid.Empty)
        {
            return SubscriptionErrors.PlanRequired;
        }

        if (price < 0)
        {
            return SubscriptionErrors.InvalidPrice;
        }

        if (expiresAtUtc <= startsAtUtc)
        {
            return SubscriptionErrors.InvalidDateRange;
        }

        return new Subscription(Guid.CreateVersion7(), tenantId, planId, interval, price, currency, startsAtUtc, expiresAtUtc);
    }

    public void Cancel()
    {
        Status = SubscriptionStatus.Cancelled;
    }

    public void MarkExpired()
    {
        Status = SubscriptionStatus.Expired;
    }
}
