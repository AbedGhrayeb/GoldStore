using Application.Abstractions.Messaging;
using Application.Abstractions.Services;
using Domain.Common;
using SharedKernel.Result;

namespace Application.Features.SalesInvoices.GetNextNumber;

internal sealed class GetNextInvoiceNumberQueryHandler(IInvoiceNumberService invoiceNumberService)
    : IQueryHandler<GetNextInvoiceNumberQuery, string>
{
    public async Task<Result<string>> Handle(GetNextInvoiceNumberQuery query, CancellationToken cancellationToken)
    {
        return await invoiceNumberService.PeekAsync(
            InvoiceDocumentType.Sales, cancellationToken);
    }
}
