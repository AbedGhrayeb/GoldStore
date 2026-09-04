using Domain.Common;
using Domain.Inventory;
using SharedKernel;

namespace Domain.Suppliers;

public sealed class SupplierGoldLedgerEntry : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }


    public Guid SupplierId { get; private set; }

    public Karat Karat { get; private set; }

    public decimal WeightInGrams { get; private set; }

    public decimal Equivalent21KWeightInGrams { get; private set; }

    public SupplierBalanceMovementType MovementType { get; private set; }

    public SupplierGoldReferenceType ReferenceType { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public string? Notes { get; set; }
    public Supplier Supplier { get; set; }

    private SupplierGoldLedgerEntry()
    {

    }
    private SupplierGoldLedgerEntry(Guid id, Guid supplierId, Karat karat, decimal weightInGrams,
        SupplierBalanceMovementType movementType, SupplierGoldReferenceType referenceType, Guid? referenceId, string? notes) : base(id)
    {
        SupplierId = supplierId;
        Karat = karat;
        WeightInGrams = weightInGrams;
        Equivalent21KWeightInGrams = GoldWeight.CalculateEquivalent21KWeight(weightInGrams, karat);
        MovementType = movementType;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Notes = notes;
    }

    public static SupplierGoldLedgerEntry Create(Guid supplierId, Karat karat, decimal weightInGrams,
        SupplierBalanceMovementType movementType, SupplierGoldReferenceType referenceType, Guid? referenceId, string? notes)
    {
        return new SupplierGoldLedgerEntry(Guid.CreateVersion7(), supplierId, karat, weightInGrams,
            movementType, referenceType, referenceId, notes);
    }
}
