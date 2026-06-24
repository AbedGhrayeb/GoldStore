using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.Supplier;

public sealed class CreateSupplierModel
{
    [Required(ErrorMessage = "اسم المورد مطلوب")]
    [StringLength(200, ErrorMessage = "اسم المورد لا يمكن أن يتجاوز 200 حرف")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم الهاتف الأساسي مطلوب")]
    [StringLength(30, ErrorMessage = "رقم الهاتف لا يمكن أن يتجاوز 30 حرف")]
    public string PrimaryPhone { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "رقم الهاتف الثانوي لا يمكن أن يتجاوز 30 حرف")]
    public string? SecondaryPhone { get; set; }

    [Required(ErrorMessage = "رقم الحساب البنكي مطلوب")]
    [StringLength(100, ErrorMessage = "رقم الحساب البنكي لا يمكن أن يتجاوز 100 حرف")]
    public string BankAccountNumber { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 1000 حرف")]
    public string? Notes { get; set; }
}
