// <copyright file="SupplierManufacturingLedgerEntry.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Common;
using SharedKernel;

namespace Domain.Suppliers;

public sealed class SupplierManufacturingLedgerEntry : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public Guid SupplierId { get; private set; }

    public decimal Amount { get; private set; }

    public Currency Currency { get; private set; }

    public SupplierBalanceMovementType MovementType { get; private set; }

    public SupplierManufacturingReferenceType ReferenceType { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public string? Notes { get; private set; }

    public Supplier Supplier { get; set; }

    private SupplierManufacturingLedgerEntry()
    {
    }

    private SupplierManufacturingLedgerEntry(Guid id, Guid supplierId, decimal amount, Currency currency,
        SupplierBalanceMovementType movementType, SupplierManufacturingReferenceType referenceType, Guid? referenceId, string? notes)
        : base(id)
    {
        this.SupplierId = supplierId;
        this.Amount = amount;
        this.Currency = currency;
        this.MovementType = movementType;
        this.ReferenceType = referenceType;
        this.ReferenceId = referenceId;
        this.Notes = notes;
    }

    public static SupplierManufacturingLedgerEntry Create(Guid supplierId, decimal amount, Currency currency,
        SupplierBalanceMovementType movementType, SupplierManufacturingReferenceType referenceType, Guid? referenceId, string? notes)
    {
        return new SupplierManufacturingLedgerEntry(Guid.CreateVersion7(), supplierId, amount, currency,
            movementType, referenceType, referenceId, notes);
    }
}
