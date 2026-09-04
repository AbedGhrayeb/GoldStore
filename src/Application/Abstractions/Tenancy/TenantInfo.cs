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
