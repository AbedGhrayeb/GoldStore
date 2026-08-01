using SharedKernel.Result;

namespace Domain.CustomerPurchases;

public static class CustomerPurchaseInvoiceItemErrors
{
    public static readonly Error InvoiceNumberRequired = Error.Validation(
        "CustomerPurchaseInvoices.InvoiceNumber",
        "رقم الفاتورة مطلوب");
    public static readonly Error CategoryIdRequired = Error.Validation(
        "CustomerPurchaseInvoices.CategoryId",
        "معرف الفئة مطلوب");
    public static readonly Error KaratRequired = Error.Validation(
        "CustomerPurchaseInvoices.Karat",
        "رقم العيار مطلوب");
    public static readonly Error CurrencyRequired = Error.Validation(
        "CustomerPurchaseInvoices.Currency",
        "العملة مطلوبة");
    public static readonly Error WeightInGramsRequired = Error.Validation(
        "CustomerPurchaseInvoices.WeightInGrams",
        "الوزن بالجرام مطلوب");
    public static readonly Error PricePerGramRequired = Error.Validation(
        "CustomerPurchaseInvoices.PricePerGram",
        "سعر الجرام مطلوب");
    public static readonly Error WeightInGramsMustBePositive = Error.Validation(
        "CustomerPurchaseInvoices.WeightInGrams",
        "الوزن بالجرام يجب أن يكون أكبر من الصفر");

    public static readonly Error PricePerGramMustBePositive = Error.Validation(
        "CustomerPurchaseInvoices.PricePerGram",
        "سعر الجرام يجب أن يكون أكبر من الصفر");
}

