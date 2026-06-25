using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.SupplierFinancialTransaction;

public class CreateSupplierFinancialTransactionModel
{
    [Required]
    public Guid SupplierId { get; set; }

    [Required]
    [Range(1, 2)]
    public int Direction { get; set; }

    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string Currency { get; set; } = "Jod";

    [Required]
    public Guid AccountId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
