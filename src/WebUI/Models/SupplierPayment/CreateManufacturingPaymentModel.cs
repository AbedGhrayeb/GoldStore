using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.SupplierPayment;

public class CreateManufacturingPaymentModel
{
    [Required(ErrorMessage = "يجب اختيار المورد")]
    public Guid SupplierId { get; set; }

    [Required(ErrorMessage = "يجب اختيار حساب الدفع")]
    public Guid AccountId { get; set; }

    [Range(0.001, double.MaxValue, ErrorMessage = "المبلغ يجب أن يكون أكبر من صفر")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "يجب اختيار العملة")]
    public string Currency { get; set; } = "JOD";

    [StringLength(1000, ErrorMessage = "ملاحظات طويلة جداً")]
    public string? Notes { get; set; }

    public List<PaymentLegModel>? PaymentLegs { get; set; }
}