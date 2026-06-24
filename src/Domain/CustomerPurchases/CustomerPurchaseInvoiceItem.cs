using Domain.Common;
using SharedKernel;

namespace Domain.CustomerPurchases;

public sealed class CustomerPurchaseInvoiceItem : Entity
{
    public Guid Id { get; set; }

    public Guid CustomerPurchaseInvoiceId { get; set; }

    public Guid? CategoryId { get; set; }

    public Karat Karat { get; set; }

    public decimal WeightInGrams { get; set; }

    public decimal Equivalent21KWeightInGrams { get; set; }

    public decimal PricePerGram { get; set; }

    public decimal GoldAmount { get; set; }
}
