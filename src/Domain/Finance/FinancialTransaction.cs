using Domain.Common;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Finance;

public sealed class FinancialTransaction : AuditableEntity
{
    public Guid AccountId { get; private set; }

    public Currency Currency { get; private set; }

    public decimal Amount { get; set; }

    public decimal? ExchangeRate { get; set; }

    public decimal? BaseAmount { get; set; }

    public FinancialTransactionType TransactionType { get; private set; }

    public FinancialReferenceType ReferenceType { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public string? Notes { get; private set; }
    // Navigation property
    public FinancialAccount FinancialAccount { get; set; }

    private FinancialTransaction()
    {

    }
    private FinancialTransaction(Guid id, Guid accountId, Currency currency, decimal amount, FinancialTransactionType transactionType,
        FinancialReferenceType referenceType, Guid? referenceId, string? notes) : base(id)
    {
        AccountId = accountId;
        Currency = currency;
        Amount = amount;
        TransactionType = transactionType;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Notes = notes;
    }
    public static Result<FinancialTransaction> Create(Guid accountId, Currency currency, decimal amount, FinancialTransactionType transactionType,
        FinancialReferenceType referenceType, Guid? referenceId, string? notes)
    {
        if (accountId == Guid.Empty)
        {
            return FinancialAccountErrors.AccountIdRequired;
        }
        if (amount <= 0)
        {
            return FinancialAccountErrors.InvalidTargetBalance;
        }
        if (referenceId == Guid.Empty)
        {
            return FinancialAccountErrors.ReferenceId;
        }
        return new FinancialTransaction(Guid.CreateVersion7(), accountId, currency, amount, transactionType, referenceType, referenceId, notes);
    }

    public Result<Updated> Update(Guid id, Guid accountId, Currency currency, decimal amount, FinancialTransactionType transactionType,
        FinancialReferenceType referenceType, Guid? referenceId, string? notes)
    {
        if (id == Guid.Empty)
        {
            return FinancialAccountErrors.NotFound(id);
        }
        if (accountId == Guid.Empty)
        {
            return FinancialAccountErrors.AccountIdRequired;
        }
        if (amount <= 0)
        {
            return FinancialAccountErrors.InvalidTargetBalance;
        }
        if (referenceId == Guid.Empty)
        {
            return FinancialAccountErrors.ReferenceId;
        }

        AccountId = accountId;
        Currency = currency;
        Amount = amount;
        TransactionType = transactionType;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Notes = notes;
        return Result.Updated;
    }
}
