using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.SupplierDelivery;

public sealed class DeliveryLineModel
{
    [Range(18, 24, ErrorMessage = "العيار غير صالح")]
    public int Karat { get; set; } = 21;

    [Range(0.01, double.MaxValue, ErrorMessage = "الوزن يجب أن يكون أكبر من صفر")]
    public decimal WeightInGrams { get; set; }
}
