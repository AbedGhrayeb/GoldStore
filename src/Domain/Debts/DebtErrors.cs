using SharedKernel.Result;

namespace Domain.Debts;

public static class DebtErrors
{
    public static Error NameRequired => Error.Validation(
        "Debts.NameRequired",
        $"اسم العميل مطلوب");

    public static Error NotFound(Guid debtId) => Error.NotFound(
        "Debts.NotFound",
        $"معرف الديون غير موجود");

    public static Error PaymentExceedsBalance(decimal amount, decimal balance) => Error.Validation(
        "Debts.PaymentExceedsBalance",
        $"مبلغ الدفع ({amount}) يتجاوز الرصيد المتبقي ({balance})");

    public static readonly Error PaymentAmountMustBePositive = Error.Validation(
        "Debts.PaymentAmountMustBePositive",
        "Payment amount must be greater than zero");

    public static Error AccountNotFound(Guid accountId) => Error.NotFound(
        "Debts.AccountNotFound",
        $"معرف الحساب المالي غير موجود");

    public static readonly Error AccountCurrencyMismatch = Error.Validation(
        "Debts.AccountCurrencyMismatch",
        "عملة الحساب المالي لا تتطابق");

    public static readonly Error NoAccountLinked = Error.Validation(
        "Debts.NoAccountLinked",
        "الحساب المالي غير متصل");

    public static Error AmountBelowPayments(decimal amount, decimal payments) => Error.Validation(
        "Debts.AmountBelowPayments",
        $"مبلغ جديد ({amount}) أقل من المبالغ المدفوعة إجماليًا ({payments})");
}
