using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.SalesInvoice;

public class CreateSalesInvoiceModel
{
    [Required]
    [StringLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? CustomerPhone { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public string Currency { get; set; } = "JOD";

    [Required]
    public List<SalesInvoiceItemModel> Items { get; set; } = [];

    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AmountPaid { get; set; }

    public int? PaymentMethod { get; set; }

    public Guid? AccountId { get; set; }

    [StringLength(50)]
    public string? BuyerAccountNumber { get; set; }

    [Required]
    public Guid EmplyeeId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
