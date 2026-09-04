using SharedKernel.Result;

namespace Domain.Employees;

public static class SalaryPaymentErrors
{
    public static readonly Error EmployeeIdRequired = Error.Validation(
        "SalaryPayment.EmployeeId.Required",
        "معرف الموظف مطلوب");

    public static readonly Error AccountIdRequired = Error.Validation(
        "SalaryPayment.AccountId.Required",
        "حساب الدفع مطلوب");

    public static Error EmployeeNotFound(Guid employeeId) => Error.NotFound(
        "SalaryPayment.Employee.NotFound",
        $"الموظف بمعرف '{employeeId}' غير موجود");

    public static readonly Error EmployeeInactive = Error.Validation(
        "SalaryPayment.Employee.Inactive",
        "لا يمكن دفع راتب لموظف متوقف");

    public static readonly Error SalaryNotSet = Error.Validation(
        "SalaryPayment.Employee.SalaryNotSet",
        "لا يوجد راتب محدد لهذا الموظف");

    public static readonly Error DailyCycleNotSupported = Error.Validation(
        "SalaryPayment.Employee.DailyCycleNotSupported",
        "دفع الراتب اليومي غير مدعوم");

    public static Error AccountNotFound(Guid accountId) => Error.NotFound(
        "SalaryPayment.Account.NotFound",
        $"حساب الدفع بمعرف '{accountId}' غير موجود");

    public static readonly Error AccountInactive = Error.Validation(
        "SalaryPayment.Account.Inactive",
        "حساب الدفع غير نشط");

    public static readonly Error AccountCurrencyMismatch = Error.Validation(
        "SalaryPayment.Account.CurrencyMismatch",
        "حساب الدفع يجب أن يكون بعملة راتب الموظف");

    public static readonly Error DiscountCannotBeNegative = Error.Validation(
        "SalaryPayment.Discount.Negative",
        "قيمة الخصم لا يمكن أن تكون سالبة");

    public static readonly Error DiscountExceedsSalary = Error.Validation(
        "SalaryPayment.Discount.ExceedsSalary",
        "قيمة الخصم يجب أن تكون أقل من الراتب");

    public static readonly Error AmountMustBePositive = Error.Validation(
        "SalaryPayment.Amount.MustBePositive",
        "مبلغ الدفعة يجب أن يكون قيمة موجبة");

    public static readonly Error AmountExceedsNet = Error.Validation(
        "SalaryPayment.Amount.ExceedsNet",
        "مبلغ الدفعة أكبر من صافي الراتب المستحق");

    public static readonly Error AmountExceedsRemaining = Error.Validation(
        "SalaryPayment.Amount.ExceedsRemaining",
        "مبلغ الدفعة أكبر من المتبقي المستحق لهذه الفترة");

    public static readonly Error PeriodFullyPaid = Error.Validation(
        "SalaryPayment.Period.FullyPaid",
        "تم دفع كامل راتب هذه الفترة");

    public static readonly Error PaymentDateRequired = Error.Validation(
        "SalaryPayment.PaymentDate.Required",
        "تاريخ الدفع مطلوب");

    public static readonly Error ScheduledDateRequired = Error.Validation(
        "SalaryPayment.ScheduledDate.Required",
        "تاريخ الاستحقاق مطلوب");
}
