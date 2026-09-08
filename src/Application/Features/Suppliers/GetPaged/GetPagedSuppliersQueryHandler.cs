// <copyright file="GetPagedSuppliersQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Application.Common.Models;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Suppliers.GetPaged;

internal sealed class GetPagedSuppliersQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetPagedSuppliersQuery, PaginatedList<SupplierResponse>>
{
    public async Task<Result<PaginatedList<SupplierResponse>>> Handle(GetPagedSuppliersQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Supplier> suppliersQuery = context.Suppliers
            .AsNoTracking()
            .Where(s => s.TenantId == currentTenant.TenantId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search.Trim();
            suppliersQuery = suppliersQuery.Where(s =>
                s.Name.Contains(search) ||
                s.PrimaryPhone.Contains(search) ||
                (s.SecondaryPhone != null && s.SecondaryPhone.Contains(search)));
        }

        if (query.ActiveOnly.HasValue)
        {
            suppliersQuery = suppliersQuery.Where(s => s.IsActive == query.ActiveOnly.Value);
        }

        int totalCount = await suppliersQuery.CountAsync(cancellationToken);

        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        List<Guid> pageIds = await suppliersQuery
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        List<Supplier> suppliers = await context.Suppliers
            .AsNoTracking()
            .Where(s => s.TenantId == currentTenant.TenantId)
            .Where(s => pageIds.Contains(s.Id))
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var goldBalances = await context.SupplierGoldLedgerEntries
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .Where(e => pageIds.Contains(e.SupplierId))
            .GroupBy(e => e.SupplierId)
            .Select(g => new
            {
                SupplierId = g.Key,
                Balance = g.Sum(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Equivalent21KWeightInGrams : -e.Equivalent21KWeightInGrams),
            })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Balance, cancellationToken);

        var manufacturingBalances = await context.SupplierManufacturingLedgerEntries
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .Where(e => pageIds.Contains(e.SupplierId))
            .GroupBy(e => new { e.SupplierId, e.Currency })
            .Select(g => new
            {
                g.Key.SupplierId,
                Currency = g.Key.Currency.ToString(),
                Balance = g.Sum(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Amount : -e.Amount),
            })
            .ToListAsync(cancellationToken);

        Dictionary<Guid, decimal> manufacturingTotals = manufacturingBalances
            .GroupBy(x => x.SupplierId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Balance));

        Dictionary<Guid, List<ManufacturingBalanceByCurrency>> manufacturingBySupplier = manufacturingBalances
            .GroupBy(x => x.SupplierId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .Select(x => new ManufacturingBalanceByCurrency
                    {
                        Currency = x.Currency,
                        Balance = x.Balance,
                    })
                    .Where(b => b.Balance != 0)
                    .ToList());

        List<DateEntry> goldDates = await context.SupplierGoldLedgerEntries
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .Where(e => pageIds.Contains(e.SupplierId) && e.CreatedAtUtc != null)
            .Select(e => new DateEntry(e.SupplierId, e.CreatedAtUtc!.Value.LocalDateTime))
            .ToListAsync(cancellationToken);

        List<DateEntry> mfgDates = await context.SupplierManufacturingLedgerEntries
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .Where(e => pageIds.Contains(e.SupplierId) && e.CreatedAtUtc != null)
            .Select(e => new DateEntry(e.SupplierId, e.CreatedAtUtc!.Value.LocalDateTime))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, DateTime> lastDates = goldDates
            .Concat(mfgDates)
            .GroupBy(t => t.SupplierId)
            .ToDictionary(g => g.Key, g => g.Max(t => t.Date));

        var pageTransactions = await context.SupplierFinancialTransactions
            .AsNoTracking()
            .Where(t => t.TenantId == currentTenant.TenantId)
            .Where(t => pageIds.Contains(t.SupplierId))
            .Select(t => new { t.Id, t.SupplierId, Currency = t.Currency.ToString() })
            .ToListAsync(cancellationToken);

        List<Guid> pageTxIds = pageTransactions.Select(t => t.Id).ToList();

        Dictionary<Guid, decimal> txBalances = pageTxIds.Count == 0
            ? []
            : await context.SupplierFinancialLedgerEntries
                .AsNoTracking()
                .Where(e => e.TenantId == currentTenant.TenantId)
                .Where(e => pageTxIds.Contains(e.SupplierFinancialTransactionId))
                .GroupBy(e => e.SupplierFinancialTransactionId)
                .Select(g => new
                {
                    TransactionId = g.Key,
                    Balance = g.Sum(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Amount : -e.Amount),
                })
                .ToDictionaryAsync(x => x.TransactionId, x => x.Balance, cancellationToken);

        Dictionary<Guid, List<FinancialBalanceByCurrency>> financialBySupplier = pageTransactions
            .GroupBy(t => t.SupplierId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(t => t.Currency)
                    .Select(cg => new FinancialBalanceByCurrency
                    {
                        Currency = cg.Key,
                        Balance = cg.Sum(t => txBalances.GetValueOrDefault(t.Id, 0m)),
                    })
                    .Where(b => b.Balance != 0)
                    .ToList());

        List<SupplierResponse> items = suppliers.Select(s => new SupplierResponse
        {
            Id = s.Id,
            Name = s.Name,
            PrimaryPhone = s.PrimaryPhone,
            SecondaryPhone = s.SecondaryPhone,
            BankAccountNumber = s.BankAccountNumber,
            CreatedAt = s.CreatedAtUtc?.LocalDateTime ?? default,
            IsActive = s.IsActive,
            Notes = s.Notes,
            GoldBalance = goldBalances.TryGetValue(s.Id, out decimal goldBalance) ? goldBalance : 0,
            ManufacturingBalance = manufacturingTotals.TryGetValue(s.Id, out decimal mfgBalance) ? mfgBalance : 0,
            ManufacturingBalancesByCurrency = manufacturingBySupplier.TryGetValue(s.Id, out List<ManufacturingBalanceByCurrency>? manufacturing) ? manufacturing : [],
            FinancialBalancesByCurrency = financialBySupplier.TryGetValue(s.Id, out List<FinancialBalanceByCurrency>? financial) ? financial : [],
            LastTransactionDate = lastDates.TryGetValue(s.Id, out DateTime lastDate) ? lastDate : null,
        }).ToList();

        return new PaginatedList<SupplierResponse>(items, page, pageSize, totalCount);
    }

    private sealed record DateEntry(Guid SupplierId, DateTime Date);
}
