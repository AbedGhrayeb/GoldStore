using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.Finance;

public class SetBalanceModel
{
    [Required(ErrorMessage = "معرف الحساب مطلوب")]
    public Guid AccountId { get; set; }

    [Required(ErrorMessage = "الرصيد المستهدف مطلوب")]
    [Range(0, double.MaxValue, ErrorMessage = "الرصيد المستهدف لا يمكن أن يكون سالباً")]
    public decimal TargetBalance { get; set; }

    [StringLength(500, ErrorMessage = "الملاحظات طويلة جداً")]
    public string? Notes { get; set; }
}
