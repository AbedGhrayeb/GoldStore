using Domain.Common;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Inventory;

public sealed class InventoryAdjustment : AuditableEntity
{

    public InventoryAdjustmentType Type { get; private set; }

    public Karat Karat { get; private set; }

    public decimal WeightInGrams { get; private set; }

    public decimal Equivalent21KWeightInGrams { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public string? Notes { get; private set; }

    private InventoryAdjustment()
    {

    }
    private InventoryAdjustment(Guid id, InventoryAdjustmentType type, Karat karat,
            decimal weightInGrams, string reason, string? notes) : base(id)
    {
        Karat = karat;
        WeightInGrams = weightInGrams;
        Equivalent21KWeightInGrams = GoldWeight.CalculateEquivalent21KWeight(WeightInGrams, Karat);
        Reason = reason;
        Notes = notes;
    }

    public static Result<InventoryAdjustment> Create(InventoryAdjustmentType type, Karat karat,
            decimal weightInGrams, string reason, string? notes)
    {
        if (weightInGrams <= 0)
        {
            return InvenToryAdjustmentErrors.WeightMustbeGreaterThanZero;
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            return InvenToryAdjustmentErrors.ReasonRequired;
        }
        return new InventoryAdjustment(Guid.CreateVersion7(), type, karat, weightInGrams, reason, notes);
    }
}
