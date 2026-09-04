namespace Application.Features.Subscriptions;

public sealed record SubscriptionPlanSnapshot(
    Guid Id,
    string Name,
    string Key,
    int? MaximumActiveUsers,
    int? MaximumPostedInvoicesPerPeriod,
    int? MaximumActiveBranches,
    long? MaximumStorageBytes,
    bool IsActive,
    bool IsTrial,
    int DurationInMonths,
    decimal Price,
    decimal? DiscountPercent,
    decimal EffectivePrice);

public sealed record TenantSubscriptionResponse(
    Guid Id,
    Guid TenantId,
    string Status,
    string BillingCycle,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? BillingProvider,
    string? BillingProviderReference,
    SubscriptionPlanSnapshot Plan);
