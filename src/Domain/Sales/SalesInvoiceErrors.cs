using SharedKernel;

namespace Domain.Sales;

public static class SalesInvoiceErrors
{
    public static Error NotFound(Guid invoiceId) => Error.NotFound(
        "SalesInvoices.NotFound",
        $"The sales invoice with Id = '{invoiceId}' was not found");

    public static readonly Error NoItems = Error.Problem(
        "SalesInvoices.NoItems",
        "The invoice must contain at least one item");

    public static readonly Error InvalidPaymentAmount = Error.Problem(
        "SalesInvoices.InvalidPaymentAmount",
        "Payment amount cannot exceed the total amount");
}
