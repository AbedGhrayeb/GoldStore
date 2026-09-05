// <copyright file="TenantInfo.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Tenants;

namespace Application.Abstractions.Tenancy;

public sealed record TenantInfo(
    Guid TenantId,
    string Subdomain,
    string SchemaName,
    TenantStatus Status,
    string? ConnectionString,
    DateTimeOffset? TrialEndsAtUtc,
    DateTimeOffset? SubscriptionExpiresAtUtc);
