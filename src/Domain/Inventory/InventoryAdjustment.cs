using Domain.Common;
using SharedKernel;

namespace Domain.Inventory;

public sealed class InventoryAdjustment : Entity
{
    public Guid Id { get; set; }

    public InventoryAdjustmentType Type { get; set; }

    public Karat Karat { get; set; }

    public decimal WeightInGrams { get; set; }

    public decimal Equivalent21KWeightInGrams { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public Guid UserId { get; set; }

    public DateTime Date { get; set; }
}