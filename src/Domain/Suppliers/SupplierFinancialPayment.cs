using Domain.Finance;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Suppliers;

public sealed class SupplierFinancialPayment : AuditableEntity
{

    public Guid SupplierFinancialTransactionId { get; private set; }

    public decimal Amount { get; private set; }

    public Guid AccountId { get; private set; }

    public string? Notes { get; private set; }
    public SupplierFinancialTransaction SupplierFinancialTransaction { get; set; }
    public FinancialAccount FinancialAccount { get; set; }

    private SupplierFinancialPayment()
    {

    }
    private SupplierFinancialPayment(Guid id, Guid supplierFinancialTransactionId, decimal amount,
        Guid accountId, string? notes) : base(id)
    {
        SupplierFinancialTransactionId = supplierFinancialTransactionId;
        Amount = amount;
        AccountId = accountId;
        Notes = notes;
    }
    public static Result<SupplierFinancialPayment> Create(Guid supplierFinancialTransactionId, decimal amount, Guid accountId, string? notes)
    {
        if (supplierFinancialTransactionId == Guid.Empty)
        {
            return SupplierFinancialErrors.NotFound(supplierFinancialTransactionId);
        }
        if (accountId == Guid.Empty)
        {
            return FinancialAccountErrors.NotFound(accountId);
        }
        if (amount <= 0)
        {
            return SupplierFinancialErrors.PaymentAmountMustBePositive;
        }
        return new SupplierFinancialPayment(Guid.CreateVersion7(), supplierFinancialTransactionId, amount, accountId, notes);
    }
}
