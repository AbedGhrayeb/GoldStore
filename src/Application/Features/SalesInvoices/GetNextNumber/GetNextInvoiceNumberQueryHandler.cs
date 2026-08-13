using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.SalesInvoices.GetNextNumber;

internal sealed class GetNextInvoiceNumberQueryHandler(
    IApplicationDbContext context,
    ICurrentTenant currentTenant)
    : IQueryHandler<GetNextInvoiceNumberQuery, string>
{
    public async Task<Result<string>> Handle(GetNextInvoiceNumberQuery query, CancellationToken cancellationToken)
    {
        string yearMonth = DateTime.UtcNow.ToString("yyyy-MM");

        int count = await context.SalesInvoices
            .CountAsync(i => i.TenantId == currentTenant.TenantId && i.InvoiceNumber.StartsWith($"INV-{yearMonth}"), cancellationToken);

        string nextNumber = $"INV-{yearMonth}-{count + 1:D4}";

        return nextNumber;
    }
}
