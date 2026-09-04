using Application.Abstractions.Messaging;

namespace Application.Features.SubscriptionPlans.Update;

public sealed record UpdateSubscriptionPlanCommand(
    Guid Id,
    string Name,
    string? Key,
    int? MaximumActiveUsers,
    int? MaximumPostedInvoicesPerPeriod,
    int? MaximumActiveBranches,
    long? MaximumStorageBytes,
    bool IsTrial,
    int DurationInMonths,
    decimal Price,
    decimal? DiscountPercent,
    bool IsActive) : ICommand<Guid>;
