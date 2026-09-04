namespace Domain.Sales;

public enum SalesInvoiceStatus
{
    Draft = 1,
    Completed = 2,
    PartiallyPaid = 3,
    Cancelled = 4
}
public static class SalesInvoiceStatusExtensions
{
    public static string ToStatusLabel(this SalesInvoiceStatus status)
    {
        return status switch
        {
            SalesInvoiceStatus.Draft => "مسودة",
            SalesInvoiceStatus.Completed => "مكتملة",
            SalesInvoiceStatus.PartiallyPaid => "مدفوعة جزئياً",
            SalesInvoiceStatus.Cancelled => "ملغاة",
            _ => status.ToString()
        };
    }
}
