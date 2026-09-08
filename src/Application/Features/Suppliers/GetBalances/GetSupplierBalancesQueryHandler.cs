// <copyright file="GetSupplierBalancesQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Application.Suppliers;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Suppliers.GetBalances;

internal sealed class GetSupplierBalancesQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetSupplierBalancesQuery, SupplierBalancesResponse>
{
    public async Task<Result<SupplierBalancesResponse>> Handle(GetSupplierBalancesQuery query, CancellationToken cancellationToken)
    {
        bool exists = await context.Suppliers
            .AsNoTracking()
            .AnyAsync(s => s.Id == query.Id && s.TenantId == currentTenant.TenantId, cancellationToken);

        if (!exists)
        {
            return SupplierErrors.NotFound(query.Id);
        }

        // Grouped in memory: the per-karat / per-currency dues are tiny lists and this
        // avoids enum GroupBy translation quirks across providers.
        List<SupplierGoldLedgerEntry> goldEntries = await context.SupplierGoldLedgerEntries
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId && e.SupplierId == query.Id)
            .ToListAsync(cancellationToken);

        List<SupplierManufacturingLedgerEntry> manufacturingEntries = await context.SupplierManufacturingLedgerEntries
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId && e.SupplierId == query.Id)
            .ToListAsync(cancellationToken);

        List<SupplierGoldDue> goldByKarat = goldEntries
            .GroupBy(e => e.Karat)
            .Select(g => new SupplierGoldDue
            {
                Karat = (int)g.Key,
                NetWeight = g.Sum(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.WeightInGrams : -e.WeightInGrams),
            })
            .OrderBy(x => x.Karat)
            .ToList();

        List<SupplierManufacturingDue> manufacturingByCurrency = manufacturingEntries
            .GroupBy(e => e.Currency)
            .Select(g => new SupplierManufacturingDue
            {
                Currency = g.Key.ToString(),
                NetAmount = g.Sum(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Amount : -e.Amount),
            })
            .OrderBy(x => x.Currency)
            .ToList();

        return new SupplierBalancesResponse
        {
            SupplierId = query.Id,
            GoldByKarat = goldByKarat,
            ManufacturingByCurrency = manufacturingByCurrency,
        };
    }
}
