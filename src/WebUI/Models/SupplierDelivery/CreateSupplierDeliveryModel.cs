using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.SupplierDelivery;

public sealed class CreateSupplierDeliveryModel
{
    [Required(ErrorMessage = "يجب اختيار المورد")]
    public Guid SupplierId { get; set; }

    [Required(ErrorMessage = "يجب إضافة صنف واحد على الأقل")]
    public List<DeliveryLineModel> Lines { get; set; } = [new()];

    [Range(0, double.MaxValue, ErrorMessage = "أجور التصنيع لا يمكن أن تكون سالبة")]
    public decimal ManufacturingFeePerGram { get; set; }

    [Required(ErrorMessage = "يجب اختيار العملة")]
    public string ManufacturingFeeCurrency { get; set; } = "JOD";

    [StringLength(1000, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 1000 حرف")]
    public string? Notes { get; set; }
}
