using Application.Abstractions.Messaging;
using Domain.Tenants;

namespace Application.Features.Platform.Tenants.ProvisionTenant;

public sealed record ProvisionTenantCommand(
    string Name,
    string Subdomain,
    Guid PlanId,
    string AdminEmail,
    string AdminPassword,
    string AdminFirstName,
    string AdminLastName,
    BillingInterval Interval = BillingInterval.Monthly,
    bool StartWithTrial = false) : ICommand<Guid>;
