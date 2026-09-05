// <copyright file="GetSubscriptionPlansQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Features.SubscriptionPlans;

namespace Application.Features.SubscriptionPlans.GetSubscriptionPlans;

public sealed record GetSubscriptionPlansQuery : IQuery<IReadOnlyList<SubscriptionPlanResponse>>;
