using SharedKernel;

namespace Domain.Suppliers;

public sealed class SupplierFinancialPayment : Entity
{
    public Guid Id { get; set; }

    public Guid SupplierFinancialTransactionId { get; set; }

    public decimal Amount { get; set; }

    public Guid AccountId { get; set; }

    public DateTime Date { get; set; }

    public string? Notes { get; set; }
}
