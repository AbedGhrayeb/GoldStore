using Application.Abstractions.Messaging;
using Domain.Tenants;

namespace Application.Tenants.UpdateStatus;

/// <summary>
/// Host-only suspension and lifecycle workflow (plan Phase 5 item 7). Moves a tenant
/// between <see cref="TenantStatus.Trial"/>, <see cref="TenantStatus.Active"/>, and
/// <see cref="TenantStatus.Cancelled"/>. <see cref="TransitionAtUtc"/> carries the trial
/// end date for a trial transition or the read-only grace end for cancellation.
/// </summary>
public sealed record UpdateTenantStatusCommand(
    Guid TenantId,
    TenantStatus NewStatus,
    DateTimeOffset? TransitionAtUtc) : ICommand<Guid>;
