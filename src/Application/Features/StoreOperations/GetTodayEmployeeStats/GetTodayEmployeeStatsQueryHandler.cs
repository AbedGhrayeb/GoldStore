using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Application.Features.StoreOperations.GetKpis;
using Domain.Common;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Result;

namespace Application.Features.StoreOperations.GetTodayEmployeeStats;

internal sealed class GetTodayEmployeeStatsQueryHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    ICurrentTenant currentTenant)
    : IQueryHandler<GetTodayEmployeeStatsQuery, List<EmployeeDayStatsResponse>>
{
    public async Task<Result<List<EmployeeDayStatsResponse>>> Handle(
        GetTodayEmployeeStatsQuery query,
        CancellationToken cancellationToken)
    {
        DateTime todayStart = dateTimeProvider.Now.Date;

        var salesHeaders = await context.SalesInvoices
            .AsNoTracking()
            .Where(s => s.TenantId == currentTenant.TenantId)
            .Where(s => s.Date >= todayStart && s.Status != SalesInvoiceStatus.Cancelled)
            .Select(s => new { s.Id, s.EmployeeId, s.Currency, s.TotalAmount })
            .ToListAsync(cancellationToken);

        var purchaseHeaders = await context.CustomerPurchaseInvoices
            .AsNoTracking()
            .Where(p => p.TenantId == currentTenant.TenantId)
            .Where(p => p.Date >= todayStart)
            .Select(p => new { p.Id, p.EmployeeId, p.Currency, p.TotalAmount })
            .ToListAsync(cancellationToken);

        List<Guid> salesInvoiceIds = salesHeaders.Select(s => s.Id).ToList();
        List<Guid> purchaseInvoiceIds = purchaseHeaders.Select(p => p.Id).ToList();

        Dictionary<Guid, decimal> salesItemWeights = salesInvoiceIds.Count == 0
            ? []
            : await context.SalesInvoiceItems
                .AsNoTracking()
                .Where(i => i.TenantId == currentTenant.TenantId)
                .Where(i => salesInvoiceIds.Contains(i.SalesInvoiceId))
                .GroupBy(i => i.SalesInvoiceId)
                .Select(g => new { SalesInvoiceId = g.Key, Weight = g.Sum(i => i.Equivalent21KWeightInGrams) })
                .ToDictionaryAsync(x => x.SalesInvoiceId, x => x.Weight, cancellationToken);

        Dictionary<Guid, decimal> purchaseItemWeights = purchaseInvoiceIds.Count == 0
            ? []
            : await context.CustomerPurchaseInvoiceItems
                .AsNoTracking()
                .Where(i => i.TenantId == currentTenant.TenantId)
                .Where(i => purchaseInvoiceIds.Contains(i.CustomerPurchaseInvoiceId))
                .GroupBy(i => i.CustomerPurchaseInvoiceId)
                .Select(g => new { CustomerPurchaseInvoiceId = g.Key, Weight = g.Sum(i => i.Equivalent21KWeightInGrams) })
                .ToDictionaryAsync(x => x.CustomerPurchaseInvoiceId, x => x.Weight, cancellationToken);

        var employeeIds = salesHeaders
            .Concat(purchaseHeaders)
            .Select(h => h.EmployeeId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, string> employeeNames = await context.Employees
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.FirstName, e.LastName })
            .ToDictionaryAsync(e => e.Id, e => $"{e.FirstName} {e.LastName}", cancellationToken);

        var accumulators = new Dictionary<Guid, EmployeeAccumulator>();

        foreach (var header in salesHeaders)
        {
            if (!header.EmployeeId.HasValue)
            {
                continue;
            }

            if (!accumulators.TryGetValue(header.EmployeeId.Value, out EmployeeAccumulator? accumulator))
            {
                accumulator = new EmployeeAccumulator();
                accumulators[header.EmployeeId.Value] = accumulator;
            }

            accumulator.SalesCount++;
            accumulator.SalesWeight21K += salesItemWeights.GetValueOrDefault(header.Id);
            accumulator.SalesMoney[header.Currency] = accumulator.SalesMoney.GetValueOrDefault(header.Currency) + header.TotalAmount;
        }

        foreach (var header in purchaseHeaders)
        {
            if (!header.EmployeeId.HasValue)
            {
                continue;
            }

            if (!accumulators.TryGetValue(header.EmployeeId.Value, out EmployeeAccumulator? accumulator))
            {
                accumulator = new EmployeeAccumulator();
                accumulators[header.EmployeeId.Value] = accumulator;
            }

            accumulator.PurchasesCount++;
            accumulator.PurchasesWeight21K += purchaseItemWeights.GetValueOrDefault(header.Id);
            accumulator.PurchasesMoney[header.Currency] = accumulator.PurchasesMoney.GetValueOrDefault(header.Currency) + header.TotalAmount;
        }

        var result = accumulators
            .OrderByDescending(kv => kv.Value.SalesWeight21K + kv.Value.PurchasesWeight21K)
            .Select(kv => new EmployeeDayStatsResponse
            {
                EmployeeId = kv.Key,
                EmployeeName = employeeNames.GetValueOrDefault(kv.Key, "غير معروف"),
                SalesCount = kv.Value.SalesCount,
                SalesWeight21K = kv.Value.SalesWeight21K,
                SalesTotals = ToCurrencyTotals(kv.Value.SalesMoney),
                PurchasesCount = kv.Value.PurchasesCount,
                PurchasesWeight21K = kv.Value.PurchasesWeight21K,
                PurchasesTotals = ToCurrencyTotals(kv.Value.PurchasesMoney)
            })
            .ToList();

        return result;
    }

    private static List<CurrencyTotal> ToCurrencyTotals(Dictionary<Currency, decimal> amounts)
    {
        return amounts
            .Select(x => new CurrencyTotal(
                x.Key.ToCurrencyString(),
                CurrencyExtensions.CurrencyLabels.GetValueOrDefault(x.Key).Symbol ?? x.Key.ToString(),
                x.Value))
            .OrderByDescending(x => x.Amount)
            .ToList();
    }

    private sealed class EmployeeAccumulator
    {
        public int SalesCount { get; set; }
        public decimal SalesWeight21K { get; set; }
        public Dictionary<Currency, decimal> SalesMoney { get; } = [];
        public int PurchasesCount { get; set; }
        public decimal PurchasesWeight21K { get; set; }
        public Dictionary<Currency, decimal> PurchasesMoney { get; } = [];
    }
}
