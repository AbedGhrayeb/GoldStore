using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.SupplierFinancialTransaction;

public class CreateSupplierFinancialPaymentModel
{
    [Required]
    public Guid TransactionId { get; set; }

    [Required]
    public Guid AccountId { get; set; }

    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
