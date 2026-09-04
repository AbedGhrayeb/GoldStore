using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenancy;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.TenantSettings.GetTenantSettings;

internal sealed class GetTenantSettingsQueryHandler(IPlatformDbContext platform, ITenantContext tenantContext)
    : IQueryHandler<GetTenantSettingsQuery, JsonElement>
{
    public async Task<Result<JsonElement>> Handle(GetTenantSettingsQuery query, CancellationToken cancellationToken)
    {
        Tenant? tenant = await platform.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == tenantContext.TenantId, cancellationToken);

        if (tenant is null)
        {
            return TenantErrors.NotFound(tenantContext.TenantId);
        }

        return ParseSettings(tenant.SettingsJson);
    }

    private static JsonElement ParseSettings(string? settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            using JsonDocument empty = JsonDocument.Parse("{}");
            return empty.RootElement.Clone();
        }

        using JsonDocument document = JsonDocument.Parse(settingsJson);
        return document.RootElement.Clone();
    }
}
