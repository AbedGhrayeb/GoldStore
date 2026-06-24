using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.CustomerPurchaseInvoice;

public class CustomerPurchaseInvoiceItemModel
{
    public Guid? CategoryId { get; set; }

    [Required]
    public int Karat { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal WeightInGrams { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal PricePerGram { get; set; }
}
