using Application.Abstractions.Messaging;
using Domain.Tenants;

namespace Application.Tenants.Provision;

/// <summary>
/// Host-only onboarding workflow (plan Phase 5 item 7). Creates a new tenant together
/// with its settings, subscription, administrator user, role assignment, and seed
/// financial accounts in a single transaction.
/// </summary>
public sealed record ProvisionTenantCommand(
    string Name,
    string Key,
    string TimeZoneId,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminPassword,
    Guid SubscriptionPlanId,
    SubscriptionBillingCycle BillingCycle,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc) : ICommand<Guid>;
