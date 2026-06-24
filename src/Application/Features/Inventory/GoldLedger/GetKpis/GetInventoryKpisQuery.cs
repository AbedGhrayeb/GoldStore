using Application.Abstractions.Messaging;

namespace Application.Features.Inventory.GoldLedger.GetKpis;

public sealed record GetInventoryKpisQuery : IQuery<InventoryKpiResponse>;