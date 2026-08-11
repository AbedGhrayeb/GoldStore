using Domain.Catalog;
using Domain.Common;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.CustomerPurchases;

public sealed class CustomerPurchaseInvoiceItem : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }


    public Guid CustomerPurchaseInvoiceId { get; set; }

    public Guid? CategoryId { get; set; }

    public Karat Karat { get; private set; }

    public decimal WeightInGrams { get; private set; }

    public decimal Equivalent21KWeightInGrams { get; private set; }

    public decimal PricePerGram { get; private set; }

    public decimal GoldAmount => WeightInGrams * PricePerGram;

    // Navigation properties
    public CustomerPurchaseInvoice CustomerPurchaseInvoice { get; set; }
    public Category? Category { get; set; } = null;

    public CustomerPurchaseInvoiceItem()
    {

    }
    public CustomerPurchaseInvoiceItem(Guid id, Guid? categoryId, Karat karat, decimal weightInGrams, decimal pricePerGram) : base(id)
    {
        CategoryId = categoryId;
        Karat = karat;
        WeightInGrams = weightInGrams;
        Equivalent21KWeightInGrams = GoldWeight.CalculateEquivalent21KWeight(weightInGrams, karat);
        PricePerGram = pricePerGram;
    }
    public static Result<CustomerPurchaseInvoiceItem> Create(Guid? categoryId, Karat karat, decimal weightInGrams, decimal pricePerGram)
    {

        if (categoryId == Guid.Empty)
        {
            return CustomerPurchaseInvoiceItemErrors.CategoryIdRequired;
        }

        if (weightInGrams <= 0)
        {
            return CustomerPurchaseInvoiceItemErrors.WeightInGramsMustBePositive;
        }

        if (pricePerGram <= 0)
        {
            return CustomerPurchaseInvoiceItemErrors.PricePerGramMustBePositive;
        }

        return new CustomerPurchaseInvoiceItem(Guid.CreateVersion7(), categoryId, karat, weightInGrams, pricePerGram);
    }
}
