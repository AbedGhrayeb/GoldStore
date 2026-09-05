// <copyright file="TenantSubscription.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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
        DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, string? billingProvider, string? billingProviderReference)
        : base(id)
    {
        this.TenantId = tenantId;
        this.SubscriptionPlanId = subscriptionPlanId;
        this.Status = SubscriptionStatus.Active;
        this.BillingCycle = billingCycle;
        this.StartsAtUtc = startsAtUtc;
        this.EndsAtUtc = endsAtUtc;
        this.BillingProvider = billingProvider;
        this.BillingProviderReference = billingProviderReference;
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

        this.StartsAtUtc = startsAtUtc;
        this.EndsAtUtc = endsAtUtc;
        this.Status = SubscriptionStatus.Active;

        return Result.Updated;
    }

    public Result<Updated> ChangePlan(Guid newPlanId)
    {
        if (newPlanId == Guid.Empty)
        {
            return TenantErrors.IdRequired;
        }

        this.SubscriptionPlanId = newPlanId;
        return Result.Updated;
    }

    public void ChangeBillingCycle(SubscriptionBillingCycle billingCycle)
    {
        this.BillingCycle = billingCycle;
    }

    public void MarkPastDue()
    {
        this.Status = SubscriptionStatus.PastDue;
    }

    public void Cancel()
    {
        this.Status = SubscriptionStatus.Cancelled;
    }

    public void Expire()
    {
        this.Status = SubscriptionStatus.Expired;
    }
}
