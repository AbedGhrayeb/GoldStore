using Domain.Common;
using Domain.Finance;
using Domain.Suppliers;
using SharedKernel;

namespace Domain.SupplierOperations;

public sealed class SupplierManufacturingPayment : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public Guid SupplierId { get; private set; }

    public Guid AccountId { get; private set; }

    public decimal Amount { get; private set; }

    public Currency Currency { get; private set; }

    public string? Notes { get; private set; }

    public Supplier Supplier { get; set; }
    public FinancialAccount Account { get; set; }
    private SupplierManufacturingPayment()
    {
    }
    private SupplierManufacturingPayment(Guid id, Guid supplierId, Guid accountId, decimal amount, Currency currency, string? notes) : base(id)
    {
        SupplierId = supplierId;
        AccountId = accountId;
        Amount = amount;
        Currency = currency;
        Notes = notes;
    }
    public static SupplierManufacturingPayment Create(Guid supplierId, Guid accountId, decimal amount, Currency currency, string? notes)
    {
        return new SupplierManufacturingPayment(Guid.CreateVersion7(), supplierId, accountId, amount, currency, notes);
    }
}
