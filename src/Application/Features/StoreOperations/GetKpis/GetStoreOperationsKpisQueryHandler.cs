// <copyright file="GetStoreOperationsKpisQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Caching;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Common;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Result;

namespace Application.Features.StoreOperations.GetKpis;

internal sealed class GetStoreOperationsKpisQueryHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    ICurrentTenant currentTenant,
    ICacheService cache)
    : IQueryHandler<GetStoreOperationsKpisQuery, StoreOperationsKpiResponse>
{
    public async Task<Result<StoreOperationsKpiResponse>> Handle(
        GetStoreOperationsKpisQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsAvailable)
        {
            return await this.BuildAsync(cancellationToken);
        }

        Guid tenantId = currentTenant.TenantId;
        if (currentTenant is ICurrentTenantSetter setter)
        {
            setter.Set(tenantId, currentTenant.TenantKey);
        }

        string cacheKey = CacheKeys.Kpi(tenantId, "store-operations");
        StoreOperationsKpiResponse response = await cache.GetOrCreateAsync(
            cacheKey,
            [CacheKeys.KpiTenant(tenantId)],
            (ct) => this.BuildAsync(ct),
            CacheKeys.KpiExpiration,
            cancellationToken);

        return response;
    }

    private async Task<StoreOperationsKpiResponse> BuildAsync(CancellationToken cancellationToken)
    {
        DateTime todayStart = DateTime.SpecifyKind(dateTimeProvider.UtcNow.Date, DateTimeKind.Utc);

        var sales = await context.SalesInvoices
            .AsNoTracking()
            .Where(s => s.Date >= todayStart && s.Status != SalesInvoiceStatus.Cancelled)
            .Select(s => new { s.Id, s.Currency, s.TotalAmount })
            .ToListAsync(cancellationToken);

        var purchases = await context.CustomerPurchaseInvoices
            .AsNoTracking()
            .Where(p => p.Date >= todayStart)
            .Select(p => new { p.Id, p.Currency, p.TotalAmount })
            .ToListAsync(cancellationToken);

        var salesTotals = sales
            .GroupBy(s => s.Currency)
            .Select(g => new CurrencyTotal(
                CurrencyExtensions.CurrencyLabels.GetValueOrDefault(g.Key).Code ?? g.Key.ToString(),
                CurrencyExtensions.CurrencyLabels.GetValueOrDefault(g.Key).Symbol ?? g.Key.ToString(),
                g.Sum(s => s.TotalAmount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var purchaseTotals = purchases
            .GroupBy(p => p.Currency)
            .Select(g => new CurrencyTotal(
                CurrencyExtensions.CurrencyLabels.GetValueOrDefault(g.Key).Code ?? g.Key.ToString(),
                CurrencyExtensions.CurrencyLabels.GetValueOrDefault(g.Key).Symbol ?? g.Key.ToString(),
                g.Sum(p => p.TotalAmount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new StoreOperationsKpiResponse
        {
            TodaySalesCount = sales.Count,
            TodayPurchasesCount = purchases.Count,
            TodaySalesTotals = salesTotals,
            TodayPurchasesTotals = purchaseTotals,
            SalesByCategory = await this.BuildSalesByCategoryAsync(todayStart, cancellationToken),
            PurchasesByCategory = await this.BuildPurchasesByCategoryAsync(todayStart, cancellationToken),
        };
    }

    private async Task<List<CategoryKpi>> BuildSalesByCategoryAsync(DateTime todayStart, CancellationToken cancellationToken)
    {
        var rows = await (
            from item in context.SalesInvoiceItems.AsNoTracking()
            join invoice in context.SalesInvoices.AsNoTracking() on item.SalesInvoiceId equals invoice.Id
            join category in context.Categories.AsNoTracking() on item.CategoryId equals category.Id into categories
            from category in categories.DefaultIfEmpty()
            where invoice.Date >= todayStart && invoice.Status != SalesInvoiceStatus.Cancelled
            select new CategoryKpiRow(
                item.CategoryId,
                category != null ? category.Name : null,
                item.WeightInGrams,
                invoice.Currency,
                invoice.TotalAmount,
                invoice.Id))
            .ToListAsync(cancellationToken);

        return this.GroupByCategory(rows);
    }

    private async Task<List<CategoryKpi>> BuildPurchasesByCategoryAsync(DateTime todayStart, CancellationToken cancellationToken)
    {
        var rows = await (
            from item in context.CustomerPurchaseInvoiceItems.AsNoTracking()
            join invoice in context.CustomerPurchaseInvoices.AsNoTracking() on item.CustomerPurchaseInvoiceId equals invoice.Id
            join category in context.Categories.AsNoTracking() on item.CategoryId equals category.Id into categories
            from category in categories.DefaultIfEmpty()
            where invoice.Date >= todayStart
            select new CategoryKpiRow(
                item.CategoryId,
                category != null ? category.Name : null,
                item.WeightInGrams,
                invoice.Currency,
                invoice.TotalAmount,
                invoice.Id))
            .ToListAsync(cancellationToken);

        return this.GroupByCategory(rows);
    }

    private sealed record CategoryKpiRow(
        Guid? CategoryId,
        string? CategoryName,
        decimal WeightInGrams,
        Currency Currency,
        decimal TotalAmount,
        Guid InvoiceId);

    private List<CategoryKpi> GroupByCategory(List<CategoryKpiRow> rows)
    {
        return rows
            .GroupBy(r => r.CategoryId)
            .Select(g =>
            {
                string name = g.First().CategoryName ?? "بدون تصنيف";
                var totals = g
                    .GroupBy(r => r.Currency)
                    .Select(cg => new CurrencyTotal(
                        CurrencyExtensions.CurrencyLabels.GetValueOrDefault(cg.Key).Code ?? cg.Key.ToString(),
                        CurrencyExtensions.CurrencyLabels.GetValueOrDefault(cg.Key).Symbol ?? cg.Key.ToString(),
                        cg.GroupBy(r => r.InvoiceId).Select(ig => ig.First().TotalAmount).Sum()))
                    .OrderByDescending(x => x.Amount)
                    .ToList();

                return new CategoryKpi(
                    g.Key,
                    name,
                    g.Sum(r => r.WeightInGrams),
                    g.Count(),
                    totals);
            })
            .OrderByDescending(x => x.WeightInGrams)
            .ToList();
    }
}
