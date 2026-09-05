// <copyright file="InventoryAdjustmentType.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Domain.Inventory;

public enum InventoryAdjustmentType
{
    Increase = 1,
    Decrease = 2,
    Damage = 3,
    Loss = 4,
    Correction = 5,
}

public static class InventoryAdjustmentTypeExtensions
{
    public static string GetTypeLabel(this InventoryAdjustmentType adjustmentType)
    {
        return adjustmentType switch
        {
            InventoryAdjustmentType.Increase => "زيادة",
            InventoryAdjustmentType.Decrease => "نقصان",
            InventoryAdjustmentType.Damage => "تلف",
            InventoryAdjustmentType.Loss => "خسارة",
            InventoryAdjustmentType.Correction => "يدوي تصحيح",
            _ => adjustmentType.ToString(),
        };
    }

    public static (string Color, string Bg, string Icon) GetTypeStyling(this InventoryAdjustmentType type) => type switch
    {
        InventoryAdjustmentType.Increase => ("text-on-primary-container", "bg-primary-container/20", "arrow_upward"),
        InventoryAdjustmentType.Decrease => ("text-error", "bg-error-container/30", "arrow_downward"),
        InventoryAdjustmentType.Damage => ("text-error", "bg-error-container/30", "broken_image"),
        InventoryAdjustmentType.Loss => ("text-error", "bg-error-container/30", "warning"),
        InventoryAdjustmentType.Correction => ("text-on-surface", "bg-surface-container", "edit"),
        _ => ("text-secondary", "bg-surface-container", "sync_alt"),
    };
}
