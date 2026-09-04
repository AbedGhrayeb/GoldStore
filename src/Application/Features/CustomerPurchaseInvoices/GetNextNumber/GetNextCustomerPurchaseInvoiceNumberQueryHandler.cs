using Application.Abstractions.Messaging;
using Application.Abstractions.Services;
using Domain.Common;
using SharedKernel.Result;

namespace Application.Features.CustomerPurchaseInvoices.GetNextNumber;

internal sealed class GetNextCustomerPurchaseInvoiceNumberQueryHandler(IInvoiceNumberService invoiceNumberService)
    : IQueryHandler<GetNextCustomerPurchaseInvoiceNumberQuery, string>
{
    public async Task<Result<string>> Handle(GetNextCustomerPurchaseInvoiceNumberQuery query, CancellationToken cancellationToken)
    {
        return await invoiceNumberService.PeekAsync(
            InvoiceDocumentType.CustomerPurchase, cancellationToken);
    }
}
