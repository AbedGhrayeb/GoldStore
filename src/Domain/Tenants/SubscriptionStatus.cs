// <copyright file="SubscriptionStatus.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Domain.Tenants;

public enum SubscriptionStatus
{
    Active,
    PastDue,
    Cancelled,
    Expired,
}
