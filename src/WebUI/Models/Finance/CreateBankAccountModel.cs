using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.Finance;

public class CreateBankAccountModel
{
    [Required(ErrorMessage = "اسم الحساب مطلوب")]
    [StringLength(200, ErrorMessage = "اسم الحساب طويل جداً")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "يجب اختيار العملة")]
    public string Currency { get; set; } = "Jod";

    [StringLength(50, ErrorMessage = "رقم الحساب طويل جداً")]
    public string? AccountNumber { get; set; }

    [StringLength(500, ErrorMessage = "الملاحظات طويلة جداً")]
    public string? Notes { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "الرصيد الافتتاحي لا يمكن أن يكون سالباً")]
    public decimal OpeningBalance { get; set; } = 0m;
}