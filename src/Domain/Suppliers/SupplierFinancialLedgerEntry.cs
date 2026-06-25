using SharedKernel;

namespace Domain.Suppliers;

public sealed class SupplierFinancialLedgerEntry : Entity
{
    public Guid Id { get; set; }

    public Guid SupplierFinancialTransactionId { get; set; }

    public decimal Amount { get; set; }

    public SupplierBalanceMovementType MovementType { get; set; }

    public DateTime Date { get; set; }

    public string? Notes { get; set; }
}
