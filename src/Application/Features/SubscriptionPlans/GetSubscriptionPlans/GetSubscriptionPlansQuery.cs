using Application.Abstractions.Messaging;
using Application.Features.SubscriptionPlans;

namespace Application.Features.SubscriptionPlans.GetSubscriptionPlans;

public sealed record GetSubscriptionPlansQuery : IQuery<IReadOnlyList<SubscriptionPlanResponse>>;
