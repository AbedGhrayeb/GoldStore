using Domain.Common;

namespace Application.Abstractions.Services;

public interface IInvoiceNumberService
{
    Task<string> AllocateAsync(
        InvoiceDocumentType documentType,
        CancellationToken cancellationToken = default);

    Task<string> PeekAsync(
        InvoiceDocumentType documentType,
        CancellationToken cancellationToken = default);
}
