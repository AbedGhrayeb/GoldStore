using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Result;

namespace Application.Features.SalesInvoices.GetKpis;

internal sealed class GetSalesInvoiceKpisQueryHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    ICurrentTenant currentTenant)
    : IQueryHandler<GetSalesInvoiceKpisQuery, SalesInvoiceKpiResponse>
{
    public async Task<Result<SalesInvoiceKpiResponse>> Handle(GetSalesInvoiceKpisQuery query, CancellationToken cancellationToken)
    {
        DateTime todayStart = dateTimeProvider.UtcNow.Date;

        List<SalesInvoice> todayInvoices = await context.SalesInvoices
            .AsNoTracking()
            .Where(i => i.TenantId == currentTenant.TenantId && i.Date >= todayStart && i.Status != SalesInvoiceStatus.Cancelled)
            .ToListAsync(cancellationToken);

        int count = todayInvoices.Count;
        decimal totalSales = todayInvoices.Sum(i => i.TotalAmount);
        decimal totalPaid = todayInvoices.Sum(i => i.AmountPaid);
        decimal totalRemaining = todayInvoices.Sum(i => i.RemainingBalance);

        return new SalesInvoiceKpiResponse
        {
            TodayCount = count,
            TotalSales = totalSales,
            TotalSalesDisplay = totalSales.ToString("N3"),
            TotalPaid = totalPaid,
            TotalPaidDisplay = totalPaid.ToString("N3"),
            TotalRemaining = totalRemaining,
            TotalRemainingDisplay = totalRemaining.ToString("N3")
        };
    }
}
