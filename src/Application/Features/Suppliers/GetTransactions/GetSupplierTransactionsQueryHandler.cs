// <copyright file="GetSupplierTransactionsQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Application.Common.Models;
using Application.Suppliers;
using Domain.Common;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Suppliers.GetTransactions;

internal sealed record FinancialTxCurrency(Guid Id, Currency Currency);

internal sealed class GetSupplierTransactionsQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetSupplierTransactionsQuery, PaginatedList<SupplierTransactionResponse>>
{
    public async Task<Result<PaginatedList<SupplierTransactionResponse>>> Handle(GetSupplierTransactionsQuery query, CancellationToken cancellationToken)
    {
        bool exists = await context.Suppliers
            .AsNoTracking()
            .AnyAsync(s => s.Id == query.SupplierId && s.TenantId == currentTenant.TenantId, cancellationToken);

        if (!exists)
        {
            return SupplierErrors.NotFound(query.SupplierId);
        }

        string? type = query.Type?.Trim().ToLowerInvariant();
        bool includeGold = string.IsNullOrEmpty(type) || type == "gold";
        bool includeManufacturing = string.IsNullOrEmpty(type) || type == "manufacturing";
        bool includeFinancial = string.IsNullOrEmpty(type) || type == "financial";

        // Small per-supplier lists grouped in memory: avoids cross-table union paging
        // complexity while staying tenant- and supplier-scoped.
        var transactions = new List<SupplierTransactionResponse>();

        if (includeGold)
        {
            List<SupplierGoldLedgerEntry> goldEntries = await context.SupplierGoldLedgerEntries
                .AsNoTracking()
                .Where(e => e.TenantId == currentTenant.TenantId && e.SupplierId == query.SupplierId)
                .ToListAsync(cancellationToken);

            transactions.AddRange(goldEntries.Select(e => new SupplierTransactionResponse
            {
                Id = e.Id,
                Description = e.ReferenceType == SupplierGoldReferenceType.SupplierDelivery ? "توريد ذهب" :
                              e.ReferenceType == SupplierGoldReferenceType.SupplierScrapPayment ? "استلام كسر ذهب" :
                              "تسوية يدوية",
                Date = e.CreatedAtUtc?.LocalDateTime ?? DateTime.MinValue,
                Type = "ذهب",
                Amount = e.Equivalent21KWeightInGrams,
                Unit = "جم",
                Direction = e.MovementType == SupplierBalanceMovementType.Increase ? "+" : "-",
            }));
        }

        if (includeManufacturing)
        {
            List<SupplierManufacturingLedgerEntry> manufacturingEntries = await context.SupplierManufacturingLedgerEntries
                .AsNoTracking()
                .Where(e => e.TenantId == currentTenant.TenantId && e.SupplierId == query.SupplierId)
                .ToListAsync(cancellationToken);

            transactions.AddRange(manufacturingEntries.Select(e => new SupplierTransactionResponse
            {
                Id = e.Id,
                Description = e.ReferenceType == SupplierManufacturingReferenceType.SupplierDelivery ? "أجور تصنيع" :
                              e.ReferenceType == SupplierManufacturingReferenceType.SupplierManufacturingPayment ? "دفعة نقدية - أجور" :
                              "تسوية يدوية",
                Date = e.CreatedAtUtc?.LocalDateTime ?? DateTime.MinValue,
                Type = "تصنيع",
                Amount = e.Amount,
                Unit = e.Currency.ToString(),
                Direction = e.MovementType == SupplierBalanceMovementType.Increase ? "+" : "-",
            }));
        }

        if (includeFinancial)
        {
            // Currency converts in memory: enum ToString() is not SQL-translatable.
            List<FinancialTxCurrency> financialTxCurrencies = await context.SupplierFinancialTransactions
                .AsNoTracking()
                .Where(t => t.TenantId == currentTenant.TenantId && t.SupplierId == query.SupplierId)
                .Select(t => new FinancialTxCurrency(t.Id, t.Currency))
                .ToListAsync(cancellationToken);

            Dictionary<Guid, string> financialCurrencies = financialTxCurrencies
                .ToDictionary(x => x.Id, x => x.Currency.ToString());

            if (financialCurrencies.Count != 0)
            {
                List<Guid> financialTxIds = financialCurrencies.Keys.ToList();

                List<SupplierFinancialLedgerEntry> financialEntries = await context.SupplierFinancialLedgerEntries
                    .AsNoTracking()
                    .Where(e => e.TenantId == currentTenant.TenantId)
                    .Where(e => financialTxIds.Contains(e.SupplierFinancialTransactionId))
                    .ToListAsync(cancellationToken);

                transactions.AddRange(financialEntries.Select(e => new SupplierTransactionResponse
                {
                    Id = e.Id,
                    Description = e.MovementType == SupplierBalanceMovementType.Increase ? "سلفة" : "دفعة سلفة",
                    Date = e.CreatedAtUtc?.LocalDateTime ?? DateTime.MinValue,
                    Type = "مالي",
                    Amount = e.Amount,
                    Unit = financialCurrencies.GetValueOrDefault(e.SupplierFinancialTransactionId, string.Empty),
                    Direction = e.MovementType == SupplierBalanceMovementType.Increase ? "+" : "-",
                }));
            }
        }

        List<SupplierTransactionResponse> ordered = transactions
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToList();

        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 50);

        List<SupplierTransactionResponse> items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedList<SupplierTransactionResponse>(items, page, pageSize, ordered.Count);
    }
}
