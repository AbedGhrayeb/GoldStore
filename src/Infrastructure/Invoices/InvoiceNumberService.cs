using System.Globalization;
using Application.Abstractions.Services;
using Domain.Common;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Invoices;

internal sealed class InvoiceNumberService(ApplicationDbContext context) : IInvoiceNumberService
{
    private const int MaximumAllocationAttempts = 5;

    public async Task<string> AllocateAsync(
        InvoiceDocumentType documentType,
        CancellationToken cancellationToken = default)
    {
        string period = DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        Exception? lastConcurrencyException = null;

        for (int attempt = 1; attempt <= MaximumAllocationAttempts; attempt++)
        {
            InvoiceNumberSequence? sequence = await context.InvoiceNumberSequences
                .SingleOrDefaultAsync(
                    item => item.DocumentType == documentType && item.Period == period,
                    cancellationToken);

            int allocatedNumber;
            if (sequence is null)
            {
                allocatedNumber = await GetNextNumberFromInvoicesAsync(documentType, period, cancellationToken);
                sequence = InvoiceNumberSequence.Create(documentType, period, allocatedNumber + 1);
                context.InvoiceNumberSequences.Add(sequence);
            }
            else
            {
                allocatedNumber = sequence.AllocateNext();
            }

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return Format(documentType, period, allocatedNumber);
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < MaximumAllocationAttempts)
            {
                lastConcurrencyException = ex;
                context.ChangeTracker.Clear();
            }
            catch (DbUpdateException ex) when (attempt < MaximumAllocationAttempts)
            {
                // Concurrent first use can race on the tenant/document/period unique index.
                lastConcurrencyException = ex;
                context.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException(
            "Could not allocate a unique invoice number after repeated concurrency conflicts.",
            lastConcurrencyException);
    }

    public async Task<string> PeekAsync(
        InvoiceDocumentType documentType,
        CancellationToken cancellationToken = default)
    {
        string period = DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        int? nextNumber = await context.InvoiceNumberSequences
            .AsNoTracking()
            .Where(item => item.DocumentType == documentType && item.Period == period)
            .Select(item => (int?)item.NextNumber)
            .SingleOrDefaultAsync(cancellationToken);

        int number = nextNumber
            ?? await GetNextNumberFromInvoicesAsync(documentType, period, cancellationToken);

        return Format(documentType, period, number);
    }

    private async Task<int> GetNextNumberFromInvoicesAsync(
        InvoiceDocumentType documentType,
        string period,
        CancellationToken cancellationToken)
    {
        string prefix = Prefix(documentType, period);
        List<string> invoiceNumbers = documentType switch
        {
            InvoiceDocumentType.Sales => await context.SalesInvoices
                .AsNoTracking()
                .Where(invoice => invoice.InvoiceNumber.StartsWith(prefix))
                .Select(invoice => invoice.InvoiceNumber)
                .ToListAsync(cancellationToken),
            InvoiceDocumentType.CustomerPurchase => await context.CustomerPurchaseInvoices
                .AsNoTracking()
                .Where(invoice => invoice.InvoiceNumber.StartsWith(prefix))
                .Select(invoice => invoice.InvoiceNumber)
                .ToListAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, null),
        };

        int maximum = invoiceNumbers
            .Select(number => ParseSequence(number, prefix))
            .DefaultIfEmpty(0)
            .Max();

        return maximum + 1;
    }

    private static int ParseSequence(string invoiceNumber, string prefix) =>
        int.TryParse(
            invoiceNumber.AsSpan(prefix.Length),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out int sequence)
            ? sequence
            : 0;

    private static string Format(
        InvoiceDocumentType documentType,
        string period,
        int number) => $"{Prefix(documentType, period)}{number:D4}";

    private static string Prefix(InvoiceDocumentType documentType, string period) => documentType switch
    {
        InvoiceDocumentType.Sales => $"INV-{period}-",
        InvoiceDocumentType.CustomerPurchase => $"PUR-{period}-",
        _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, null),
    };
}
