namespace Application.Features.SubscriptionPlans;

public sealed record SubscriptionPlanResponse(
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
