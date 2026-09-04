using SharedKernel;

namespace Domain.Suppliers;

public sealed class SupplierFinancialLedgerEntry : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }


    public Guid SupplierFinancialTransactionId { get; set; }

    public decimal Amount { get; set; }

    public SupplierBalanceMovementType MovementType { get; set; }

    public string? Notes { get; set; }

    private SupplierFinancialLedgerEntry()
    {

    }
    private SupplierFinancialLedgerEntry(Guid id, Guid supplierFinancialTransactionId, decimal amount, SupplierBalanceMovementType movementType, string? notes) : base(id)
    {
        SupplierFinancialTransactionId = supplierFinancialTransactionId;
        Amount = amount;
        MovementType = movementType;
        Notes = notes;
    }

    public static SupplierFinancialLedgerEntry Create(Guid supplierFinancialTransactionId, decimal amount, SupplierBalanceMovementType movementType, string? notes)
    {

        return new SupplierFinancialLedgerEntry(Guid.CreateVersion7(), supplierFinancialTransactionId, amount, movementType, notes);
    }
}
