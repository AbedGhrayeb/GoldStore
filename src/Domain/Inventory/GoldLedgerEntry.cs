using Domain.Common;
using SharedKernel;

namespace Domain.Inventory;

public sealed class GoldLedgerEntry : Entity
{
    public Guid Id { get; set; }

    public Karat Karat { get; set; }

    public decimal WeightInGrams { get; set; }

    public decimal Equivalent21KWeightInGrams { get; set; }

    public GoldMovementType MovementType { get; set; }

    public GoldReferenceType ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public Guid UserId { get; set; }

    public DateTime Date { get; set; }

    public string? Notes { get; set; }
}
