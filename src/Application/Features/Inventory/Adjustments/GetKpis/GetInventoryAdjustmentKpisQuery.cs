using Application.Abstractions.Messaging;

namespace Application.Features.Inventory.Adjustments.GetKpis;

public sealed record GetInventoryAdjustmentKpisQuery : IQuery<InventoryAdjustmentKpiResponse>;