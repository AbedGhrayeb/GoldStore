using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Tenants.UpdateStatus;

/// <summary>
/// Enforces the tenant lifecycle state machine (plan Phase 0 item 3): Pending tenants
/// cannot sign in, cancelled tenants get a read-only grace window, and re-activation
/// restores full access. Cancellation also cancels the tenant's active subscription.
/// </summary>
internal sealed class UpdateTenantStatusCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateTenantStatusCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UpdateTenantStatusCommand command, CancellationToken cancellationToken)
    {
        Tenant? tenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == command.TenantId, cancellationToken);
        if (tenant is null)
        {
            return TenantErrors.NotFound(command.TenantId);
        }

        if (command.NewStatus == TenantStatus.Active)
        {
            if (tenant.Status == TenantStatus.Active)
            {
                return tenant.Id;
            }

            if (tenant.Status is not (TenantStatus.Pending or TenantStatus.Trial or TenantStatus.Cancelled))
            {
                return TenantErrors.InvalidTransition(tenant.Status, TenantStatus.Active);
            }

            tenant.Activate();
        }
        else if (command.NewStatus == TenantStatus.Trial)
        {
            if (tenant.Status is not (TenantStatus.Pending or TenantStatus.Active))
            {
                return TenantErrors.InvalidTransition(tenant.Status, TenantStatus.Trial);
            }

            if (command.TransitionAtUtc is not { } trialEndsAtUtc)
            {
                return TenantErrors.TransitionDateRequired(TenantStatus.Trial);
            }

            Result<Updated> startTrial = tenant.StartTrial(trialEndsAtUtc);
            if (startTrial.IsError)
            {
                return startTrial.Errors;
            }
        }
        else if (command.NewStatus == TenantStatus.Cancelled)
        {
            if (tenant.Status is not (TenantStatus.Pending or TenantStatus.Trial or TenantStatus.Active))
            {
                return TenantErrors.InvalidTransition(tenant.Status, TenantStatus.Cancelled);
            }

            if (command.TransitionAtUtc is not { } readOnlyUntilUtc)
            {
                return TenantErrors.TransitionDateRequired(TenantStatus.Cancelled);
            }

            Result<Updated> cancel = tenant.Cancel(readOnlyUntilUtc);
            if (cancel.IsError)
            {
                return cancel.Errors;
            }

            await CancelActiveSubscriptionAsync(tenant.Id, cancellationToken);
        }
        else
        {
            return TenantErrors.InvalidTransition(tenant.Status, command.NewStatus);
        }

        await context.SaveChangesAsync(cancellationToken);

        return tenant.Id;
    }

    private async Task CancelActiveSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        // Host-only infrastructure: tenant-owned rows are invisible to the global query
        // filter without an ambient tenant, so the active subscription is located
        // explicitly with IgnoreQueryFilters (plan Phase 3 item 8).
        TenantSubscription? subscription = await context.TenantSubscriptions
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);

        subscription?.Cancel();
    }
}
