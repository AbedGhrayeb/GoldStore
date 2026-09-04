using Application.Abstractions.Messaging;

namespace Application.Features.SalesInvoices.GetNextNumber;

public sealed record GetNextInvoiceNumberQuery : IQuery<string>;
