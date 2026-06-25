using Domain.Common;
using SharedKernel;

namespace Domain.Suppliers;

public sealed class SupplierFinancialTransaction : Entity
{
    public Guid Id { get; set; }

    public Guid SupplierId { get; set; }

    public SupplierFinancialTransactionDirection Direction { get; set; }

    public decimal Amount { get; set; }

    public Currency Currency { get; set; }

    public Guid AccountId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}
