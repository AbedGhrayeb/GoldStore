using Domain.Common;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Finance;

public sealed class FinancialAccount : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public string Name { get; private set; }

    public Currency Currency { get; private set; }

    public FinancialAccountType AccountType { get; private set; }

    public string? AccountNumber { get; private set; }

    public string? Notes { get; private set; }

    private FinancialAccount()
    {

    }
    private FinancialAccount(Guid id, string name, Currency currency, FinancialAccountType accountType, string? accountNumber, string? notes) : base(id)
    {
        Name = name;
        Currency = currency;
        AccountType = accountType;
        AccountNumber = accountNumber;
        Notes = notes;
    }

    public static Result<FinancialAccount> Create(string name, Currency currency, FinancialAccountType accountType, string? accountNumber, string? notes)
    {
        if (string.IsNullOrEmpty(name))
        {
            return FinancialAccountErrors.AccountNameRequired;
        }
        return new FinancialAccount(Guid.CreateVersion7(), name, currency, accountType, accountNumber, notes);
    }
    public Result<Updated> Update(Guid id, string name, Currency currency, FinancialAccountType accountType, string? accountNumber, string? notes)
    {
        if (id == Guid.Empty)
        {
            return FinancialAccountErrors.AccountIdRequired;
        }
        if (string.IsNullOrEmpty(name))
        {
            return FinancialAccountErrors.AccountNameRequired;
        }
        Name = name;
        Currency = currency;
        AccountType = accountType;
        AccountNumber = accountNumber;
        Notes = notes;
        return Result.Updated;
    }
}
