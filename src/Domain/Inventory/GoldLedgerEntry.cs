using Domain.Common;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Inventory;

public sealed class GoldLedgerEntry : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }


    public Karat Karat { get; private set; }

    public decimal WeightInGrams { get; private set; }

    public decimal Equivalent21KWeightInGrams { get; private set; }

    public GoldMovementType MovementType { get; private set; }

    public GoldReferenceType ReferenceType { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public string? Notes { get; private set; }
    private GoldLedgerEntry()
    {

    }
    private GoldLedgerEntry(Guid id, Karat karat, decimal weightInGrams,
        GoldMovementType movementType, GoldReferenceType referenceType, Guid? referenceId, string? notes) : base(id)
    {
        Karat = karat;
        WeightInGrams = weightInGrams;
        Equivalent21KWeightInGrams = GoldWeight.CalculateEquivalent21KWeight(weightInGrams, karat);
        MovementType = movementType;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Notes = notes;
    }

    public static Result<GoldLedgerEntry> Create(Karat karat, decimal weightInGrams,
        GoldMovementType movementType, GoldReferenceType referenceType, Guid? referenceId, string? notes)
    {

        return new GoldLedgerEntry(Guid.CreateVersion7(), karat, weightInGrams,
            movementType, referenceType, referenceId, notes);
    }
}
