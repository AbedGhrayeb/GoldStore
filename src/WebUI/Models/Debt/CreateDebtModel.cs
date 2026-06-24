using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.Debt;

public class CreateDebtModel
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [Required]
    public int Direction { get; set; }

    [Required]
    public string Currency { get; set; } = "Jod";

    public Guid? AccountId { get; set; }

    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal Amount { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public DateTime Date { get; set; }
}
