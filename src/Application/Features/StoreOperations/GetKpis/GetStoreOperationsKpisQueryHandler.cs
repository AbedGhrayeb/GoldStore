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
            .Select(s => new { s.Currency, s.TotalAmount })
            .ToListAsync(cancellationToken);

        var purchases = await context.CustomerPurchaseInvoices
            .AsNoTracking()
            .Where(p => p.Date >= todayStart)
            .Select(p => new { p.Currency, p.TotalAmount })
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
        };
    }
}
