// <copyright file="InventoryAdjustmentResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Features.Inventory.Adjustments;

public sealed record InventoryAdjustmentResponse
{
    public Guid Id { get; init; }

    public DateTime Date { get; init; }

    public string Type { get; init; } = string.Empty;

    public string TypeLabel { get; init; } = string.Empty;

    public string TypeColor { get; init; } = string.Empty;

    public string TypeBg { get; init; } = string.Empty;

    public string TypeIcon { get; init; } = string.Empty;

    public string Karat { get; init; }

    public decimal WeightInGrams { get; init; }

    public decimal SignedWeight { get; init; }

    public decimal Equivalent21KWeightInGrams { get; init; }

    public string Reason { get; init; } = string.Empty;

    public string? Notes { get; init; }

    public string UserName { get; init; } = string.Empty;
}
