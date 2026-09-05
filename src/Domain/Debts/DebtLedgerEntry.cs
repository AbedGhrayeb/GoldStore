// <copyright file="DebtLedgerEntry.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel;
using SharedKernel.Result;

namespace Domain.Debts;

public sealed class DebtLedgerEntry : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public Guid DebtId { get; private set; }

    public decimal Amount { get; private set; }

    public DebtBalanceMovementType MovementType { get; private set; }

    public string? Notes { get; private set; }

    // Navigation property
    public Debt Debt { get; set; } = null!;

    private DebtLedgerEntry()
    {
    }

    private DebtLedgerEntry(Guid id, Guid debtId, decimal amount, DebtBalanceMovementType movementType, string? notes)
        : base(id)
    {
        this.DebtId = debtId;
        this.Amount = amount;
        this.MovementType = movementType;
        this.Notes = notes;
    }

    public static Result<DebtLedgerEntry> Create(Guid debtId, decimal amount, DebtBalanceMovementType movementType, string? notes)
    {
        if (debtId == Guid.Empty)
        {
            return DebtErrors.NotFound(debtId);
        }

        if (amount <= 0)
        {
            return DebtErrors.PaymentAmountMustBePositive;
        }

        return new DebtLedgerEntry(Guid.CreateVersion7(), debtId, amount, movementType, notes);
    }
}
