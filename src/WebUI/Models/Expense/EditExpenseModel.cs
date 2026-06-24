using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.Expense;

public class EditExpenseModel
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "تاريخ المصروف مطلوب")]
    public DateTime ExpenseDate { get; set; }

    public Guid? CategoryId { get; set; }

    [StringLength(500, ErrorMessage = "الوصف طويل جداً")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "المبلغ مطلوب")]
    [Range(0.01, double.MaxValue, ErrorMessage = "المبلغ يجب أن يكون أكبر من صفر")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "يجب اختيار حساب الدفع")]
    public Guid AccountId { get; set; }
}