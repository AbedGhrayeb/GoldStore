using Application.Abstractions.Messaging;

namespace Application.Features.Platform.Tenants.GetTenants;

public sealed record GetTenantsQuery : IQuery<List<TenantSummaryResponse>>;

public sealed record TenantSummaryResponse(
    Guid Id,
    string Name,
    string Subdomain,
    string SchemaName,
    string Status,
    string PlanName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? TrialEndsAtUtc,
    DateTimeOffset? SubscriptionExpiresAtUtc);
