using Domain.Common;
using Domain.Finance;
using SharedKernel;

namespace Domain.Suppliers;

public sealed class SupplierFinancialTransaction : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public Guid SupplierId { get; private set; }

    public SupplierFinancialTransactionDirection Direction { get; private set; }

    public decimal Amount { get; private set; }

    public Currency Currency { get; private set; }

    public Guid AccountId { get; private set; }

    public string? Notes { get; private set; }

    public Supplier Supplier { get; set; }
    public FinancialAccount FinancialAccount { get; set; }

    private SupplierFinancialTransaction()
    {

    }
    private SupplierFinancialTransaction(Guid id, Guid supplierId, SupplierFinancialTransactionDirection direction,
        decimal amount, Currency currency, Guid accountId, string? notes) : base(id)
    {
        SupplierId = supplierId;
        Direction = direction;
        Amount = amount;
        Currency = currency;
        AccountId = accountId;
        Notes = notes;
    }
    public static SupplierFinancialTransaction Create(Guid supplierId, SupplierFinancialTransactionDirection direction,
        decimal amount, Currency currency, Guid accountId, string? notes)
    {
        return Create(Guid.CreateVersion7(), supplierId, direction, amount, currency, accountId, notes);
    }

    public static SupplierFinancialTransaction Create(Guid id, Guid supplierId, SupplierFinancialTransactionDirection direction,
        decimal amount, Currency currency, Guid accountId, string? notes)
    {
        return new SupplierFinancialTransaction(id, supplierId, direction, amount, currency, accountId, notes);
    }
}
