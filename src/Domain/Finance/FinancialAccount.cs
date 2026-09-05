// <copyright file="FinancialAccount.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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

    private FinancialAccount(Guid id, string name, Currency currency, FinancialAccountType accountType, string? accountNumber, string? notes)
        : base(id)
    {
        this.Name = name;
        this.Currency = currency;
        this.AccountType = accountType;
        this.AccountNumber = accountNumber;
        this.Notes = notes;
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

        this.Name = name;
        this.Currency = currency;
        this.AccountType = accountType;
        this.AccountNumber = accountNumber;
        this.Notes = notes;
        return Result.Updated;
    }
}
