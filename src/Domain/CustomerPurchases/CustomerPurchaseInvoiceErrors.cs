using SharedKernel.Result;

namespace Domain.CustomerPurchases;

public static class CustomerPurchaseInvoiceErrors
{
    public static readonly Error InvoiceNumberRequired = Error.Validation(
        "CustomerPurchaseInvoices.InvoiceNumber",
        "رقم الفاتورة مطلوب");
    public static readonly Error SellerNameRequired = Error.Validation(
        "CustomerPurchaseInvoices.SellerName",
        "اسم البائع مطلوب");
    public static readonly Error EmployeeIdRequired = Error.Validation(
        "CustomerPurchaseInvoices.Employee",
        "اسم الموظف مطلوب");
    public static readonly Error CurrencyRequired = Error.Validation(
        "CustomerPurchaseInvoices.Currency",
        "العملة مطلوبة");
    public static readonly Error PaymentMethodRequired = Error.Validation(
        "CustomerPurchaseInvoices.PaymentMethod",
        "طريقة الدفع مطلوبة");
    public static readonly Error AccountIdRequired = Error.Validation(
        "CustomerPurchaseInvoices.Account",
        "رقم الحساب مطلوب");

    public static readonly Error NoItems = Error.Validation(
        "CustomerPurchaseInvoices.NoItems",
        "يجب إضافة عناصر إلى الفاتورة");
    public static readonly Error InvalidPaymentAmount = Error.Validation(
        "CustomerPurchaseInvoices.InvalidPaymentAmount",
        "المبلغ المدفوع غير صحيح");

    public static readonly Error AccountNotFound = Error.NotFound(
        "CustomerPurchaseInvoices.AccountNotFound",
        "الحساب المطلوب غير موجود");

    public static readonly Error AccountDoesNotMatchPayment = Error.Failure(
        "CustomerPurchaseInvoices.AccountDoesNotMatchPayment",
        "الحساب المطلوب غير مطابق للدفع");

    public static readonly Error DatabaseError = Error.Failure(
        "CustomerPurchaseInvoices.DatabaseError",
        "حدث خطأ غير متوقع أثناء حفظ فاتورة الشراء");
}

