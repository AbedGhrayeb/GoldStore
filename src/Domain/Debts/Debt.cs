// <copyright file="Debt.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Common;
using Domain.Finance;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Debts;

public sealed class Debt : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public string Name { get; private set; }

    public string? Phone { get; private set; }

    public DebtDirection Direction { get; private set; }

    public Currency Currency { get; private set; }

    public Guid? AccountId { get; private set; }

    public string? Notes { get; set; }

    // Navigation property
    public FinancialAccount? FinancialAccount { get; set; } = null;

    private Debt()
    {
    }

    private Debt(Guid id, string name, string? phone, DebtDirection direction, Currency currency, Guid? accountId, string? notes)
        : base(id)
    {
        this.Name = name;
        this.Phone = phone;
        this.Direction = direction;
        this.Currency = currency;
        this.AccountId = accountId;
        this.Notes = notes;
    }

    public static Result<Debt> Create(string name, string? phone, DebtDirection direction, Currency currency, Guid? accountId, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DebtErrors.NameRequired;
        }

        if (accountId == Guid.Empty)
        {
            return DebtErrors.NoAccountLinked;
        }

        return new Debt(Guid.CreateVersion7(), name, phone, direction, currency, accountId, notes);
    }

    public Result<Updated> Update(string name, string? phone, Guid? accountId, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DebtErrors.NameRequired;
        }

        this.Name = name;
        this.Phone = phone;
        this.Notes = notes;
        this.AccountId = accountId;
        this.Notes = notes;
        return Result.Updated;
    }
}
