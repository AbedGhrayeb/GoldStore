using Domain.Common;
using Domain.Suppliers;
using SharedKernel;

namespace Domain.SupplierOperations;

public sealed class SupplierScrapGoldPayment : AuditableEntity
{

    public Guid SupplierId { get; private set; }

    public Karat Karat { get; private set; }

    public decimal WeightInGrams { get; private set; }

    public decimal Equivalent21KWeightInGrams { get; private set; }

    public string? Notes { get; private set; }
    public Supplier Supplier { get; set; }

    private SupplierScrapGoldPayment()
    {

    }
    private SupplierScrapGoldPayment(Guid id, Guid supplierId, Karat karat, decimal weightInGrams, string? notes) : base(id)
    {
        SupplierId = supplierId;
        Karat = karat;
        WeightInGrams = weightInGrams;
        Equivalent21KWeightInGrams = GoldWeight.CalculateEquivalent21KWeight(weightInGrams, karat);
        Notes = notes;
    }

    public static SupplierScrapGoldPayment Create(Guid supplierId, Karat karat, decimal weightInGrams, string? notes)
    {
        return new SupplierScrapGoldPayment(Guid.CreateVersion7(), supplierId, karat, weightInGrams, notes);
    }
}
