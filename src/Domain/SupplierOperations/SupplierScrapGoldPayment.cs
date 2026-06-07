using Domain.Common;
using SharedKernel;

namespace Domain.SupplierOperations;

public sealed class SupplierScrapGoldPayment : Entity
{
    public Guid Id { get; set; }

    public Guid SupplierId { get; set; }

    public Karat Karat { get; set; }

    public decimal WeightInGrams { get; set; }

    public decimal Equivalent21KWeightInGrams { get; set; }

    public Guid UserId { get; set; }

    public DateTime Date { get; set; }

    public string? Notes { get; set; }
}
