using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenancy;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Platform.Tenants.ChangeTenantStatus;

internal sealed class ChangeTenantStatusCommandHandler(
    IPlatformDbContext platform,
    ITenantResolver tenantResolver)
    : ICommandHandler<ChangeTenantStatusCommand, Updated>
{
    public async Task<Result<Updated>> Handle(ChangeTenantStatusCommand command, CancellationToken cancellationToken)
    {
        Tenant? tenant = await platform.Tenants
            .FirstOrDefaultAsync(t => t.Id == command.TenantId, cancellationToken);

        if (tenant is null)
        {
            return TenantErrors.NotFound(command.TenantId);
        }

        tenant.ChangeStatus(command.Status);
        await platform.SaveChangesAsync(cancellationToken);

        // Take effect immediately instead of waiting for the cached entry to expire.
        tenantResolver.Invalidate(tenant.Subdomain);

        return Result.Updated;
    }
}
