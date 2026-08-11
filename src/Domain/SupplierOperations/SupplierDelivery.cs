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


    public string? Notes { get; set; }

    public Supplier Supplier { get; set; }

    private SupplierDelivery()
    {

    }
    private SupplierDelivery(Guid id, Guid supplierId, Karat karat, decimal weightInGrams, decimal manufacturingFeePerGram, Currency manufacturingFeeCurrency, string? notes) : base(id)
    {
        SupplierId = supplierId;
        Karat = karat;
        WeightInGrams = weightInGrams;
        Equivalent21KWeightInGrams = GoldWeight.CalculateEquivalent21KWeight(weightInGrams, karat);
        ManufacturingFeePerGram = manufacturingFeePerGram;
        TotalManufacturingFee = GoldWeight.CalculateEquivalent21KWeight(weightInGrams, karat) * manufacturingFeePerGram;
        ManufacturingFeeCurrency = manufacturingFeeCurrency;
        Notes = notes;
    }

    public static SupplierDelivery Create(Guid supplierId, Karat karat, decimal weightInGrams, decimal manufacturingFeePerGram, Currency manufacturingFeeCurrency, string? notes)
    {
        return new SupplierDelivery(Guid.CreateVersion7(), supplierId, karat, weightInGrams, manufacturingFeePerGram, manufacturingFeeCurrency, notes);
    }
}
