// <copyright file="RenewTenantSubscriptionCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Domain.Tenants;

namespace Application.Features.Subscriptions.Renew;

public sealed record RenewTenantSubscriptionCommand(
    Guid TenantId,
    Guid? NewPlanId,
    SubscriptionBillingCycle BillingCycle,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc) : ICommand<Guid>;
