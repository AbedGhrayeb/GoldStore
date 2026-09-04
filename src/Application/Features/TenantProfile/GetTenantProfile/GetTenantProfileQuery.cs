using Application.Abstractions.Messaging;

namespace Application.Features.TenantProfile.GetTenantProfile;

public sealed record GetTenantProfileQuery : IQuery<TenantProfileResponse>;

public sealed record TenantProfileResponse(
    Guid Id,
    string Name,
    string Subdomain,
    string Status,
    bool IsReadOnly,
    Guid PlanId,
    string PlanName,
    string Currency,
    string? SettingsJson,
    string? BillingInterval,
    decimal? CurrentPrice,
    DateTimeOffset? CurrentPeriodStartUtc,
    DateTimeOffset? CurrentPeriodEndUtc,
    DateTimeOffset? TrialEndsAtUtc,
    DateTimeOffset? SubscriptionExpiresAtUtc,
    DateTimeOffset CreatedAtUtc);
