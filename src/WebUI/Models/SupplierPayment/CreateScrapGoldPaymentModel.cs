using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.SupplierPayment;

public class CreateScrapGoldPaymentModel
{
    [Required(ErrorMessage = "يجب اختيار المورد")]
    public Guid SupplierId { get; set; }

    [Range(18, 24, ErrorMessage = "العيار غير صالح")]
    public int Karat { get; set; }

    [Range(0.001, double.MaxValue, ErrorMessage = "الوزن يجب أن يكون أكبر من صفر")]
    public decimal WeightInGrams { get; set; }

    [StringLength(1000, ErrorMessage = "ملاحظات طويلة جداً")]
    public string? Notes { get; set; }
}