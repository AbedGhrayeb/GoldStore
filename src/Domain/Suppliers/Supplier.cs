// <copyright file="Supplier.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel;
using SharedKernel.Result;

namespace Domain.Suppliers;

public sealed class Supplier : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public string Name { get; private set; }

    public string PrimaryPhone { get; private set; }

    public string? SecondaryPhone { get; private set; }

    public string? BankAccountNumber { get; private set; }

    public string? Notes { get; private set; }

    public IEnumerable<SupplierFinancialTransaction> SupplierFinancialTransactions { get; set; } = new List<SupplierFinancialTransaction>();

    public IEnumerable<SupplierGoldLedgerEntry> SupplierGoldLedgerEntries { get; set; } = new List<SupplierGoldLedgerEntry>();

    public IEnumerable<SupplierManufacturingLedgerEntry> SupplierManufacturingLedgerEntries { get; set; } = new List<SupplierManufacturingLedgerEntry>();

    private Supplier()
    {
    }

    private Supplier(Guid id, string name, string primaryPhone, string? secondaryPhone, string? bankAccountNumber, string? notes)
    {
        this.Name = name;
        this.PrimaryPhone = primaryPhone;
        this.SecondaryPhone = secondaryPhone;
        this.BankAccountNumber = bankAccountNumber;
        this.Notes = notes;
    }

    public static Result<Supplier> Create(string name, string primaryPhone, string? secondaryPhone, string? bankAccountNumber, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return SupplierErrors.NameRequired;
        }

        if (string.IsNullOrWhiteSpace(primaryPhone))
        {
            return SupplierErrors.PrimaryPhoneRequired;
        }

        return new Supplier(Guid.CreateVersion7(), name, primaryPhone, secondaryPhone, bankAccountNumber, notes);
    }

    public Result<Updated> Update(Guid? id, string name, string primaryPhone, string? secondaryPhone, string? bankAccountNumber, string? notes)
    {
        if (id == Guid.Empty || id == null)
        {
            return SupplierErrors.NotFound(id ?? Guid.Empty);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return SupplierErrors.NameRequired;
        }

        if (string.IsNullOrWhiteSpace(primaryPhone))
        {
            return SupplierErrors.PrimaryPhoneRequired;
        }

        this.Name = name;
        this.PrimaryPhone = primaryPhone;
        this.SecondaryPhone = secondaryPhone;
        this.BankAccountNumber = bankAccountNumber;
        this.Notes = notes;
        return Result.Updated;
    }

    public Result<Updated> ActiveToggle(Guid? id, bool isActive)
    {
        if (id == Guid.Empty || id == null)
        {
            return SupplierErrors.NotFound(id ?? Guid.Empty);
        }

        this.IsActive = isActive;
        return Result.Updated;
    }
}
