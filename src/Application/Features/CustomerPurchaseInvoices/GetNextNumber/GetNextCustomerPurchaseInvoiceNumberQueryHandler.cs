using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.CustomerPurchaseInvoices.GetNextNumber;

internal sealed class GetNextCustomerPurchaseInvoiceNumberQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetNextCustomerPurchaseInvoiceNumberQuery, string>
{
    public async Task<Result<string>> Handle(GetNextCustomerPurchaseInvoiceNumberQuery query, CancellationToken cancellationToken)
    {
        string yearMonth = DateTime.UtcNow.ToString("yyyy-MM");

        int count = await context.CustomerPurchaseInvoices
            .CountAsync(i => i.TenantId == currentTenant.TenantId && i.InvoiceNumber.StartsWith($"PUR-{yearMonth}"), cancellationToken);

        return $"PUR-{yearMonth}-{count + 1:D4}";
    }
}
