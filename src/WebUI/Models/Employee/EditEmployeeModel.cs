using System.ComponentModel.DataAnnotations;
using Domain.Employees;

namespace WebUI.Models.Employee;

public sealed class EditEmployeeModel
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "اسم الموظف مطلوب")]
    [StringLength(20, ErrorMessage = "اسم الموظف لا يمكن أن يتجاوز 20 حرف")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم العائلة مطلوب")]
    [StringLength(20, ErrorMessage = "اسم العائلة لا يمكن أن يتجاوز 20 حرف")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "دور الموظف مطلوب")]
    public RoleEnum? Role { get; set; }

    [Required(ErrorMessage = "الراتب مطلوب")]
    [Range(0.001, 999999999, ErrorMessage = "الراتب يجب أن يكون قيمة موجبة")]
    public decimal? Salary { get; set; }

    [Required(ErrorMessage = "دورة الراتب مطلوبة")]
    public SalaryCycleEnum? SalaryCycle { get; set; }

    public bool IsActive { get; set; }

    public Guid? UserId { get; set; }

    public string? UserEmail { get; set; }
}
