using Application.Abstractions.Data;
using Domain.Common;
using Domain.Finance;
using Domain.Inventory;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Ledger;

public static class LedgerBalanceHelper
{
    public static Task<decimal> GetAccountBalanceAsync(this IApplicationDbContext context, Guid accountId,
        CancellationToken cancellationToken = default)
        => context.FinancialTransactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .SumAsync(t => t.TransactionType == FinancialTransactionType.Inflow ? t.Amount : -t.Amount, cancellationToken);

    public static Task<decimal> GetAccountBalanceExcludingAsync(this IApplicationDbContext context, Guid accountId,
        FinancialReferenceType referenceType, Guid referenceId, CancellationToken cancellationToken = default)
        => context.FinancialTransactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId && !(t.ReferenceType == referenceType && t.ReferenceId == referenceId))
            .SumAsync(t => t.TransactionType == FinancialTransactionType.Inflow ? t.Amount : -t.Amount, cancellationToken);

    public static Task<decimal> GetGoldStockAsync(this IApplicationDbContext context, Karat karat,
        CancellationToken cancellationToken = default)
        => context.GoldLedgerEntries
            .AsNoTracking()
            .Where(e => e.Karat == karat)
            .SumAsync(e => e.MovementType == GoldMovementType.Increase ? e.WeightInGrams : -e.WeightInGrams, cancellationToken);

    public static Task<decimal> GetSupplierGoldBalanceAsync(this IApplicationDbContext context, Guid supplierId, Karat karat,
        CancellationToken cancellationToken = default)
        => context.SupplierGoldLedgerEntries
            .AsNoTracking()
            .Where(e => e.SupplierId == supplierId && e.Karat == karat)
            .SumAsync(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.WeightInGrams : -e.WeightInGrams, cancellationToken);

    public static Task<decimal> GetSupplierManufacturingBalanceAsync(this IApplicationDbContext context, Guid supplierId, Currency currency,
        CancellationToken cancellationToken = default)
        => context.SupplierManufacturingLedgerEntries
            .AsNoTracking()
            .Where(e => e.SupplierId == supplierId && e.Currency == currency)
            .SumAsync(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Amount : -e.Amount, cancellationToken);
}
