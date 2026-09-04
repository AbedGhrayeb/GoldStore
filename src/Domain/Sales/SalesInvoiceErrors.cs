using SharedKernel.Result;

namespace Domain.Sales;

public static class SalesInvoiceErrors
{
    public static Error NotFound(Guid invoiceId) => Error.NotFound(
        "SalesInvoices.NotFound",
        $"The sales invoice with Id = '{invoiceId}' was not found");
    public static Error InvoceNumberRequired => Error.Validation(
        "SalesInvoices.InvoceNumberRequired",
        "رقم الفاتورة مطلوب");
    public static Error CustomerNameRequired => Error.Validation(
        "SalesInvoices.CustomerNameRequired",
        "اسم الزبون مطلوب");
    public static Error AccountIdRequired => Error.Validation(
        "SalesInvoices.AccountIdRequired",
        "اختر حساب الدفع مطلوب");
    public static Error EmployeeIdRequired => Error.Validation(
        "SalesInvoices.EmployeeIdRequired",
        "اختيار الموظف مطلوب");
    public static Error CategoryIdRequired => Error.Validation(
        "SalesInvoices.CategoryIdRequired",
        "اختيار الفئة مطلوب");
    public static Error WeightMustBePositive => Error.Validation(
        "SalesInvoices.WeightMustBePositive",
        "الوزن يجب أن يكون أكثر من الصفر");
    public static Error PriceMustBePositive => Error.Validation(
        "SalesInvoices.PriceMustBePositive",
        "السعر يجب أن يكون أكثر من الصفر");

    public static readonly Error NoItems = Error.Failure(
        "SalesInvoices.NoItems",
        "يجب أن تحتوي الفاتورة على منتج واحد على الأقل");

    public static readonly Error InvalidPaymentAmount = Error.Validation(
        "SalesInvoices.InvalidPaymentAmount",
        "Payment amount cannot exceed the total amount");
}
