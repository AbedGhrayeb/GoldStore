// <copyright file="SupplierDelivery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Catalog;
using Domain.Common;
using Domain.Suppliers;
using SharedKernel;

namespace Domain.SupplierOperations;

public sealed class SupplierDelivery : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public Guid SupplierId { get; private set; }

    public Karat Karat { get; private set; }

    public decimal WeightInGrams { get; private set; }

    public decimal Equivalent21KWeightInGrams { get; private set; }

    public decimal ManufacturingFeePerGram { get; private set; }

    public decimal TotalManufacturingFee { get; private set; }

    public Currency ManufacturingFeeCurrency { get; private set; }

    public Guid? CategoryId { get; private set; }

    public string? Notes { get; set; }

    public Supplier Supplier { get; set; }

    public Category? Category { get; set; }

    private SupplierDelivery()
    {
    }

    private SupplierDelivery(Guid id, Guid supplierId, Karat karat, decimal weightInGrams, decimal manufacturingFeePerGram, Currency manufacturingFeeCurrency, string? notes, Guid? categoryId = null)
        : base(id)
    {
        this.SupplierId = supplierId;
        this.Karat = karat;
        this.WeightInGrams = weightInGrams;
        this.Equivalent21KWeightInGrams = GoldWeight.CalculateEquivalent21KWeight(weightInGrams, karat);
        this.ManufacturingFeePerGram = manufacturingFeePerGram;
        this.TotalManufacturingFee = GoldWeight.CalculateEquivalent21KWeight(weightInGrams, karat) * manufacturingFeePerGram;
        this.ManufacturingFeeCurrency = manufacturingFeeCurrency;
        this.CategoryId = categoryId;
        this.Notes = notes;
    }

    public static SupplierDelivery Create(Guid supplierId, Karat karat, decimal weightInGrams, decimal manufacturingFeePerGram, Currency manufacturingFeeCurrency, string? notes, Guid? categoryId = null)
    {
        return new SupplierDelivery(Guid.CreateVersion7(), supplierId, karat, weightInGrams, manufacturingFeePerGram, manufacturingFeeCurrency, notes, categoryId);
    }
}
