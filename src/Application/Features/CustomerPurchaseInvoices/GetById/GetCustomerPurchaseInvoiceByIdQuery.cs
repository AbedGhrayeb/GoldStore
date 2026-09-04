using Application.Abstractions.Messaging;

namespace Application.Features.CustomerPurchaseInvoices.GetById;

public sealed record GetCustomerPurchaseInvoiceByIdQuery(Guid Id) : IQuery<CustomerPurchaseInvoiceResponse>;
