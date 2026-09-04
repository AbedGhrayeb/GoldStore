using Application.Abstractions.Messaging;

namespace Application.Features.SalesInvoices.GetById;

public sealed record GetSalesInvoiceByIdQuery(Guid Id) : IQuery<SalesInvoiceResponse>;
