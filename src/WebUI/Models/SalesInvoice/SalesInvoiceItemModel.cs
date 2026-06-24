using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.SalesInvoice;

public class SalesInvoiceItemModel
{
    public Guid? CategoryId { get; set; }

    [Required]
    [Range(18, 24)]
    public int Karat { get; set; } = 21;

    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal WeightInGrams { get; set; }

    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal PricePerGram { get; set; }
}
