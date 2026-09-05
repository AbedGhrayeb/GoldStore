// <copyright file="InventoryAdjustmentKpiResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Features.Inventory.Adjustments.GetKpis;

public sealed record InventoryAdjustmentKpiResponse
{
    public int TodayCount { get; init; }

    public decimal NetWeightChange { get; init; }

    public string NetWeightDisplay { get; init; } = "0.000";

    public bool IsNegative { get; init; }
}
