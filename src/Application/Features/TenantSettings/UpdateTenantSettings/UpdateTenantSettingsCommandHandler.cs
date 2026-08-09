using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenancy;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.TenantSettings.UpdateTenantSettings;

internal sealed class UpdateTenantSettingsCommandHandler(
    IPlatformDbContext platform,
    ITenantContext tenantContext)
    : ICommandHandler<UpdateTenantSettingsCommand, Updated>
{
    public async Task<Result<Updated>> Handle(UpdateTenantSettingsCommand command, CancellationToken cancellationToken)
    {
        Tenant? tenant = await platform.Tenants
            .SingleOrDefaultAsync(t => t.Id == tenantContext.TenantId, cancellationToken);

        if (tenant is null)
        {
            return TenantErrors.NotFound(tenantContext.TenantId);
        }

        Result<Updated> updateResult = tenant.UpdateSettings(command.Settings.GetRawText());
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await platform.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
