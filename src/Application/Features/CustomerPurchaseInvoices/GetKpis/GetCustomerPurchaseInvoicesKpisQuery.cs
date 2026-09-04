using Application.Abstractions.Messaging;

namespace Application.Features.CustomerPurchaseInvoices.GetKpis;

public sealed record GetCustomerPurchaseInvoicesKpisQuery : IQuery<CustomerPurchaseInvoiceKpiResponse>;
