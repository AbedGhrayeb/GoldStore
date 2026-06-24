using SharedKernel;

namespace Domain.CustomerPurchases;

public static class CustomerPurchaseInvoiceErrors
{
    public static readonly Error NoItems = Error.Problem(
        "CustomerPurchaseInvoices.NoItems",
        "ÙŠØ¬Ø¨ Ø¥Ø¶Ø§ÙØ© ØµÙ†Ù ÙˆØ§Ø­Ø¯ Ø¹Ù„Ù‰ Ø§Ù„Ø£Ù‚Ù„");

    public static readonly Error InvalidPaymentAmount = Error.Problem(
        "CustomerPurchaseInvoices.InvalidPaymentAmount",
        "Ø§Ù„Ù…Ø¨Ù„Øº Ø§Ù„Ù…Ø¯ÙÙˆØ¹ Ù„Ø§ ÙŠÙ…ÙƒÙ† Ø£Ù† ÙŠØªØ¬Ø§ÙˆØ² Ø¥Ø¬Ù…Ø§Ù„ÙŠ Ø§Ù„ÙØ§ØªÙˆØ±Ø©");

    public static readonly Error AccountNotFound = Error.Problem(
        "CustomerPurchaseInvoices.AccountNotFound",
        "Ø§Ù„Ø­Ø³Ø§Ø¨ Ø§Ù„Ù…Ø­Ø¯Ø¯ ØºÙŠØ± Ù…ÙˆØ¬ÙˆØ¯ Ø£Ùˆ ØºÙŠØ± ÙØ¹Ø§Ù„");

    public static readonly Error AccountDoesNotMatchPayment = Error.Problem(
        "CustomerPurchaseInvoices.AccountDoesNotMatchPayment",
        "Ø§Ù„Ø­Ø³Ø§Ø¨ Ø§Ù„Ù…Ø­Ø¯Ø¯ Ù„Ø§ ÙŠØ·Ø§Ø¨Ù‚ Ø§Ù„Ø¹Ù…Ù„Ø© ÙˆØ·Ø±ÙŠÙ‚Ø© Ø§Ù„Ø¯ÙØ¹");
}

