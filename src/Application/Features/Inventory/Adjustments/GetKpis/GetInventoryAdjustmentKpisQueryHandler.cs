using Application.Abstractions.Caching;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Result;

namespace Application.Features.Inventory.Adjustments.GetKpis;

internal sealed class GetInventoryAdjustmentKpisQueryHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    ICurrentTenant currentTenant,
    ICacheService cache)
    : IQueryHandler<GetInventoryAdjustmentKpisQuery, InventoryAdjustmentKpiResponse>
{
    public async Task<Result<InventoryAdjustmentKpiResponse>> Handle(GetInventoryAdjustmentKpisQuery query, CancellationToken cancellationToken)
    {
        if (!currentTenant.IsAvailable)
        {
            return await BuildAsync(cancellationToken);
        }

        Guid tenantId = currentTenant.TenantId;
        if (currentTenant is ICurrentTenantSetter setter)
        {
            setter.Set(tenantId, currentTenant.TenantKey);
        }

        string cacheKey = CacheKeys.Kpi(tenantId, "adjustments");
        InventoryAdjustmentKpiResponse response = await cache.GetOrCreateAsync(
            cacheKey,
            [CacheKeys.KpiTenant(tenantId)],
            (ct) => BuildAsync(ct),
            CacheKeys.KpiExpiration,
            cancellationToken);

        return response;
    }

    private async Task<InventoryAdjustmentKpiResponse> BuildAsync(CancellationToken cancellationToken)
    {
        DateTime todayStart = DateTime.SpecifyKind(dateTimeProvider.UtcNow.Date, DateTimeKind.Utc);

        List<InventoryAdjustment> todayAdjustments = await context.InventoryAdjustments
            .AsNoTracking()
            .Where(a => a.CreatedAtUtc >= todayStart)
            .ToListAsync(cancellationToken);

        int count = todayAdjustments.Count;

        decimal netChange = todayAdjustments.Sum(a =>
            a.Type == InventoryAdjustmentType.Increase ? a.WeightInGrams : -a.WeightInGrams);

        return new InventoryAdjustmentKpiResponse
        {
            TodayCount = count,
            NetWeightChange = netChange,
            NetWeightDisplay = (netChange >= 0 ? "+" : "") + netChange.ToString("F3"),
            IsNegative = netChange < 0
        };
    }
}
