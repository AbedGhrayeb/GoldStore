using Application.Abstractions.Messaging;

namespace Application.Features.SubscriptionPlans.Create;

public sealed record CreateSubscriptionPlanCommand(
    string Name,
    string? Key,
    int? MaximumActiveUsers,
    int? MaximumPostedInvoicesPerPeriod,
    int? MaximumActiveBranches,
    long? MaximumStorageBytes,
    bool IsTrial,
    int DurationInMonths,
    decimal Price,
    decimal? DiscountPercent) : ICommand<Guid>;
