using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.Employee;

public sealed class PaySalaryModel
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required(ErrorMessage = "يجب اختيار حساب الدفع")]
    public Guid? AccountId { get; set; }

    [Required(ErrorMessage = "مبلغ الدفعة مطلوب")]
    [Range(0.001, 999999999, ErrorMessage = "مبلغ الدفعة يجب أن يكون قيمة موجبة")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "تاريخ الدفع مطلوب")]
    public DateOnly? PaymentDate { get; set; }

    [StringLength(500, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 500 حرف")]
    public string? Notes { get; set; }
}
