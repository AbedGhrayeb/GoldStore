using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Suppliers.GetById;

internal sealed class GetSupplierByIdQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetSupplierByIdQuery, SupplierDetailResponse>
{
    public async Task<Result<SupplierDetailResponse>> Handle(GetSupplierByIdQuery query, CancellationToken cancellationToken)
    {
        Supplier supplier = await context.Suppliers
            .Include(s => s.SupplierGoldLedgerEntries)
            .Include(s => s.SupplierManufacturingLedgerEntries)
            .Include(s => s.SupplierFinancialTransactions)
            .Where(s => s.Id == query.Id)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (supplier is null)
        {
            return SupplierErrors.NotFound(query.Id);
        }
        var supplierGoldLedgerEntries = supplier.SupplierGoldLedgerEntries.ToList();
        decimal goldBalance = supplierGoldLedgerEntries
            .Sum(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Equivalent21KWeightInGrams : -e.Equivalent21KWeightInGrams);
        var supplierManufacturingLedgerEntries = supplier.SupplierManufacturingLedgerEntries.ToList();
        decimal manufacturingBalance = supplierManufacturingLedgerEntries
            .Sum(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Amount : -e.Amount);

        var financialTransactions = supplier.SupplierFinancialTransactions.ToList();

        var financialTxIds = financialTransactions.Select(t => t.Id).ToList();

        List<FinancialBalanceByCurrency> financialBalancesByCurrency = [];

        if (financialTxIds.Count != 0)
        {
            Dictionary<Guid, decimal> financialBalances = await context.SupplierFinancialLedgerEntries
                .Where(e => financialTxIds.Contains(e.SupplierFinancialTransactionId))
                .GroupBy(e => e.SupplierFinancialTransactionId)
                .Select(g => new { TransactionId = g.Key, Balance = g.Sum(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Amount : -e.Amount) })
                .ToDictionaryAsync(x => x.TransactionId, x => x.Balance, cancellationToken);

            financialBalancesByCurrency = financialTransactions
                .GroupBy(t => t.Currency)
                .Select(g => new FinancialBalanceByCurrency
                {
                    Currency = g.Key.ToString(),
                    Balance = g.Sum(t => financialBalances.GetValueOrDefault(t.Id, 0m))
                })
                .ToList();
        }

        List<SupplierTransactionResponse> recentTransactions = await GetRecentTransactions(query.Id,
            supplierGoldLedgerEntries,
            supplierManufacturingLedgerEntries,
            financialTxIds, cancellationToken);

        return new SupplierDetailResponse
        {
            Id = supplier.Id,
            Name = supplier.Name,
            PrimaryPhone = supplier.PrimaryPhone,
            SecondaryPhone = supplier.SecondaryPhone,
            BankAccountNumber = supplier.BankAccountNumber,
            Notes = supplier.Notes,
            IsActive = supplier.IsActive,
            CreatedAt = supplier.CreatedAtUtc!.Value.LocalDateTime,
            GoldBalance = goldBalance,
            ManufacturingBalance = manufacturingBalance,
            FinancialBalancesByCurrency = financialBalancesByCurrency,
            RecentTransactions = recentTransactions
        };
    }

    private async Task<List<SupplierTransactionResponse>> GetRecentTransactions(Guid supplierId,
            List<SupplierGoldLedgerEntry> supplierGoldLedgerEntries,
            List<SupplierManufacturingLedgerEntry> supplierManufacturingLedgerEntries,
            List<Guid> financialTxIds,
            CancellationToken cancellationToken)
    {
        var goldEntries = supplierGoldLedgerEntries
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(5)
            .Select(e => new SupplierTransactionResponse
            {
                Id = e.Id,
                Description = e.ReferenceType == SupplierGoldReferenceType.SupplierDelivery ? "توريد ذهب" :
                              e.ReferenceType == SupplierGoldReferenceType.SupplierScrapPayment ? "استلام كسر ذهب" :
                              "تسوية يدوية",
                Date = e.CreatedAtUtc!.Value.LocalDateTime,
                Type = "ذهب",
                Amount = e.Equivalent21KWeightInGrams,
                Unit = "جم",
                Direction = e.MovementType == SupplierBalanceMovementType.Increase ? "+" : "-"
            })
            .ToList();

        var mfgEntries = supplierManufacturingLedgerEntries
            .OrderByDescending(e => e.CreatedAtUtc!.Value.LocalDateTime)
            .Take(5)
            .Select(e => new SupplierTransactionResponse
            {
                Id = e.Id,
                Description = e.ReferenceType == SupplierManufacturingReferenceType.SupplierDelivery ? "أجور تصنيع" :
                              e.ReferenceType == SupplierManufacturingReferenceType.SupplierManufacturingPayment ? "دفعة نقدية - أجور" :
                              "تسوية يدوية",
                Date = e.CreatedAtUtc!.Value.LocalDateTime,
                Type = "تصنيع",
                Amount = e.Amount,
                Unit = "د.إ",
                Direction = e.MovementType == SupplierBalanceMovementType.Increase ? "+" : "-"
            })
            .ToList();

        List<SupplierTransactionResponse> financialEntries = await context.SupplierFinancialLedgerEntries
            .Where(e => financialTxIds.Contains(e.SupplierFinancialTransactionId))
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(5)
            .Select(e => new SupplierTransactionResponse
            {
                Id = e.Id,
                Description = e.MovementType == SupplierBalanceMovementType.Increase ? "سلفة" : "دفعة سلفة",
                Date = e.CreatedAtUtc!.Value.LocalDateTime,
                Type = "مالي",
                Amount = e.Amount,
                Unit = "د.إ",
                Direction = e.MovementType == SupplierBalanceMovementType.Increase ? "+" : "-"
            })
            .ToListAsync(cancellationToken);

        return goldEntries.Concat(mfgEntries).Concat(financialEntries)
            .OrderByDescending(t => t.Date)
            .Take(10)
            .ToList();
    }
}
