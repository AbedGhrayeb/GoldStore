using Domain.Catalog;
using Domain.Common;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Sales;

public sealed class SalesInvoiceItem : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public Guid SalesInvoiceId { get; private set; }

    public Guid? CategoryId { get; private set; }

    public Karat Karat { get; private set; }

    public decimal WeightInGrams { get; private set; }

    public decimal Equivalent21KWeightInGrams { get; private set; }

    public decimal PricePerGram { get; private set; }

    public decimal GoldAmount { get; private set; }

    public SalesInvoice SalesInvoice { get; set; }
    public Category Category { get; set; }

    private SalesInvoiceItem()
    {

    }
    private SalesInvoiceItem(Guid id, Guid saleInvoceId, Guid categoryId,
        Karat karat, decimal weightInGrams, decimal pricePerGram) : base(id)
    {
        SalesInvoiceId = saleInvoceId;
        CategoryId = categoryId;
        Karat = karat;
        WeightInGrams = weightInGrams;
        PricePerGram = pricePerGram;
        Equivalent21KWeightInGrams = GoldWeight.CalculateEquivalent21KWeight(weightInGrams, karat);
        GoldAmount = weightInGrams * pricePerGram;
    }

    public static Result<SalesInvoiceItem> Create(Guid saleInvoceId, Guid categoryId,
        Karat karat, decimal weightInGrams, decimal pricePerGram)
    {
        if (saleInvoceId == Guid.Empty)
        {
            return SalesInvoiceErrors.InvoceNumberRequired;
        }
        if (categoryId == Guid.Empty)
        {
            return SalesInvoiceErrors.CategoryIdRequired;
        }
        if (weightInGrams <= 0)
        {
            return SalesInvoiceErrors.WeightMustBePositive;
        }
        if (pricePerGram <= 0)
        {
            return SalesInvoiceErrors.PriceMustBePositive;
        }
        return new SalesInvoiceItem(Guid.CreateVersion7(), saleInvoceId, categoryId, karat, weightInGrams, pricePerGram);
    }
}
