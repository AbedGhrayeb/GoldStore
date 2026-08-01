using SharedKernel.Result;

namespace Domain.Employees;

public static class EmployeeErrors
{
    public static Error IdRequired => Error.Validation(
    "Employee.Id.Required",
    $"معرف الموظف مطلوب");
    public static Error FirstNameRequired => Error.Validation(
        "Employee.FirstName.Required",
        $"اسم الموظف مطلوب");
    public static Error LastNameRequired => Error.Validation(
        "Employee.LastName.Required",
        $"اسم العائلة للموظف مطلوب");
    public static Error SalaryMustbePositive => Error.Validation(
        "Employee.Salary.MustBePositive",
        $"الراتب للموظف يجب أن يكون قيمة موجبة");
    public static Error RoleRequired => Error.Validation(
        "Employee.Role.Required",
        $"الدور للموظف مطلوب");
    public static Error SalaryCycleRequired => Error.Validation(
        "Employee.SalaryCycle.Required",
        $"دورة الراتب للموظف مطلوبة");
    public static Error NotFound(Guid employeeId) => Error.NotFound(
        "Employee.NotFound",
        $"الموظف بمعرف '{employeeId}' غير موجود");
    public static Error UserNotFound(Guid userId) => Error.NotFound(
        "Employee.User.NotFound",
        $"المستخدم بمعرف '{userId}' غير موجود");
    public static Error UserAlreadyLinked(Guid userId) => Error.Conflict(
        "Employee.User.AlreadyLinked",
        $"المستخدم المحدد مرتبط بالفعل بموظف آخر");
}
