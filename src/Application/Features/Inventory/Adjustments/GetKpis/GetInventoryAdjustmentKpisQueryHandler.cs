using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Inventory.Adjustments.GetKpis;

internal sealed class GetInventoryAdjustmentKpisQueryHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetInventoryAdjustmentKpisQuery, InventoryAdjustmentKpiResponse>
{
    public async Task<Result<InventoryAdjustmentKpiResponse>> Handle(GetInventoryAdjustmentKpisQuery query, CancellationToken cancellationToken)
    {
        DateTime todayStart = dateTimeProvider.UtcNow.Date;

        List<InventoryAdjustment> todayAdjustments = await context.InventoryAdjustments
            .AsNoTracking()
            .Where(a => a.Date >= todayStart)
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