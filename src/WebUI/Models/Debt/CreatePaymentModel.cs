using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.Debt;

public class CreatePaymentModel
{
    [Required]
    public Guid DebtId { get; set; }

    [Required]
    public Guid AccountId { get; set; }

    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
