using Domain.Common;
using SharedKernel;

namespace Domain.SupplierOperations;

public sealed class SupplierDelivery : Entity
{
    public Guid Id { get; set; }

    public Guid SupplierId { get; set; }

    public Karat Karat { get; set; }

    public decimal WeightInGrams { get; set; }

    public decimal Equivalent21KWeightInGrams { get; set; }

    public decimal ManufacturingFeePerGram { get; set; }

    public decimal TotalManufacturingFee { get; set; }

    public Currency ManufacturingFeeCurrency { get; set; }

    public DateTime Date { get; set; }

    public string? Notes { get; set; }
}
