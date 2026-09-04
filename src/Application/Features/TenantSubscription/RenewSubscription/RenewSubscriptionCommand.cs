using Application.Abstractions.Messaging;
using Domain.Tenants;

namespace Application.Features.TenantSubscription.RenewSubscription;

public sealed record RenewSubscriptionCommand(BillingInterval Interval) : ICommand<RenewSubscriptionResponse>;

public sealed record RenewSubscriptionResponse(
    Guid SubscriptionId,
    string Interval,
    decimal Price,
    string Currency,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset ExpiresAtUtc);
