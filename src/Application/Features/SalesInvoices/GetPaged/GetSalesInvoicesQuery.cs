using Application.Abstractions.Messaging;
using Application.Common.Models;

namespace Application.Features.SalesInvoices.GetPaged;

public sealed record GetSalesInvoicesQuery(
    int Page = 1,
    int PageSize = 20,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null,
    string? Status = null) : IQuery<PaginatedList<SalesInvoiceResponse>>;
