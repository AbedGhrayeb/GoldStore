using Application.Abstractions.Messaging;

namespace Application.Features.CustomerPurchaseInvoices.GetNextNumber;

public sealed record GetNextCustomerPurchaseInvoiceNumberQuery : IQuery<string>;
