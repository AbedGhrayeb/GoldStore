using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.StoreOperations.GetKpis;

internal sealed class GetStoreOperationsKpisQueryHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetStoreOperationsKpisQuery, StoreOperationsKpiResponse>
{
    private static readonly Dictionary<Currency, (string Code, string Symbol)> CurrencyLabels = new()
    {
        [Currency.Jod] = ("Jod", "د.أ"),
        [Currency.Usd] = ("Usd", "$"),
        [Currency.Ils] = ("Ils", "₪")
    };

    public async Task<Result<StoreOperationsKpiResponse>> Handle(
        GetStoreOperationsKpisQuery query,
        CancellationToken cancellationToken)
    {
        DateTime todayStart = dateTimeProvider.Now.Date;

        var sales = await context.SalesInvoices
            .AsNoTracking()
            .Where(s => s.Date >= todayStart && s.Status != SalesInvoiceStatus.Cancelled)
            .Select(s => new { s.Currency, s.TotalAmount })
            .ToListAsync(cancellationToken);

        var purchases = await context.CustomerPurchaseInvoices
            .AsNoTracking()
            .Where(p => p.Date >= todayStart)
            .Select(p => new { p.Currency, p.TotalAmount })
            .ToListAsync(cancellationToken);

        List<CurrencyTotal> salesTotals = sales
            .GroupBy(s => s.Currency)
            .Select(g => new CurrencyTotal(
                CurrencyLabels.GetValueOrDefault(g.Key).Code ?? g.Key.ToString(),
                CurrencyLabels.GetValueOrDefault(g.Key).Symbol ?? g.Key.ToString(),
                g.Sum(s => s.TotalAmount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        List<CurrencyTotal> purchaseTotals = purchases
            .GroupBy(p => p.Currency)
            .Select(g => new CurrencyTotal(
                CurrencyLabels.GetValueOrDefault(g.Key).Code ?? g.Key.ToString(),
                CurrencyLabels.GetValueOrDefault(g.Key).Symbol ?? g.Key.ToString(),
                g.Sum(p => p.TotalAmount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new StoreOperationsKpiResponse
        {
            TodaySalesCount = sales.Count,
            TodayPurchasesCount = purchases.Count,
            TodaySalesTotals = salesTotals,
            TodayPurchasesTotals = purchaseTotals
        };
    }
}
