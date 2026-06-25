using SharedKernel;

namespace Domain.Suppliers;

public static class SupplierFinancialErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "SupplierFinancial.NotFound",
        $"The supplier financial transaction with Id = '{id}' was not found");

    public static Error SupplierNotFound(Guid supplierId) => Error.NotFound(
        "SupplierFinancial.SupplierNotFound",
        $"The supplier with Id = '{supplierId}' was not found");

    public static readonly Error SupplierNotActive = Error.Problem(
        "SupplierFinancial.SupplierNotActive",
        "المورد غير نشط");

    public static Error AccountNotFound(Guid accountId) => Error.NotFound(
        "SupplierFinancial.AccountNotFound",
        $"The financial account with Id = '{accountId}' was not found");

    public static readonly Error AccountNotActive = Error.Problem(
        "SupplierFinancial.AccountNotActive",
        "الحساب المالي غير نشط");

    public static readonly Error AccountCurrencyMismatch = Error.Problem(
        "SupplierFinancial.AccountCurrencyMismatch",
        "عملة الحساب لا تتطابق مع عملة المعاملة");

    public static Error PaymentExceedsBalance(decimal amount, decimal balance) => Error.Problem(
        "SupplierFinancial.PaymentExceedsBalance",
        $"Payment amount ({amount}) exceeds remaining balance ({balance})");

    public static readonly Error PaymentAmountMustBePositive = Error.Problem(
        "SupplierFinancial.PaymentAmountMustBePositive",
        "مبلغ الدفعة يجب أن يكون أكبر من صفر");
}
