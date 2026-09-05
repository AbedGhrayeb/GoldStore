// <copyright file="GetTenantSubscriptionQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Features.Subscriptions;

namespace Application.Features.Subscriptions.GetTenantSubscription;

public sealed record GetTenantSubscriptionQuery(Guid TenantId) : IQuery<TenantSubscriptionResponse>;
