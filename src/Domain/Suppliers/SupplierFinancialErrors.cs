using SharedKernel.Result;

namespace Domain.Suppliers;

public static class SupplierFinancialErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "SupplierFinancial.NotFound",
        $"المعاملة المالية للمورد بالمعرف '{id}' غير موجودة");

    public static Error SupplierNotFound(Guid supplierId) => Error.NotFound(
        "SupplierFinancial.SupplierNotFound",
        $"المورد بالمعرف '{supplierId}' غير موجود");

    public static readonly Error SupplierNotActive = Error.Failure(
        "SupplierFinancial.SupplierNotActive",
        "المورد غير نشط");

    public static Error AccountNotFound(Guid accountId) => Error.NotFound(
        "SupplierFinancial.AccountNotFound",
        $"الحساب المالي بالمعرف '{accountId}' غير موجود");

    public static readonly Error AccountNotActive = Error.Failure(
        "SupplierFinancial.AccountNotActive",
        "الحساب المالي غير نشط");

    public static readonly Error AccountCurrencyMismatch = Error.Failure(
        "SupplierFinancial.AccountCurrencyMismatch",
        "عملة الحساب لا تتطابق مع عملة المعاملة");

    public static Error PaymentExceedsBalance(decimal amount, decimal balance) => Error.Failure(
        "SupplierFinancial.PaymentExceedsBalance",
        $"Payment amount ({amount}) exceeds remaining balance ({balance})");

    public static readonly Error PaymentAmountMustBePositive = Error.Failure(
        "SupplierFinancial.PaymentAmountMustBePositive",
        "مبلغ الدفعة يجب أن يكون أكبر من صفر");
}
