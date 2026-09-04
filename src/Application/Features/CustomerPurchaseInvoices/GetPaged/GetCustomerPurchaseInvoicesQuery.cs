using Application.Abstractions.Messaging;
using Application.Common.Models;

namespace Application.Features.CustomerPurchaseInvoices.GetPaged;

public sealed record GetCustomerPurchaseInvoicesQuery(
    int Page = 1,
    int PageSize = 20,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null) : IQuery<PaginatedList<CustomerPurchaseInvoiceResponse>>;
