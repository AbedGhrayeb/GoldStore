using Application.Abstractions.Messaging;

namespace Application.Features.SalesInvoices.GetKpis;

public sealed record GetSalesInvoiceKpisQuery : IQuery<SalesInvoiceKpiResponse>;
