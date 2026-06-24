using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.InventoryAdjustment;

public class CreateInventoryAdjustmentModel
{
    [Required]
    public int AdjustmentType { get; set; }

    [Required]
    public int Karat { get; set; }

    [Range(0, double.MaxValue)]
    public decimal WeightInGrams { get; set; }

    [Required]
    [StringLength(200)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public DateTime Date { get; set; }
}