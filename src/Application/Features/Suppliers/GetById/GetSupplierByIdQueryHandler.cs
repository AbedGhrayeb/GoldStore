using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Suppliers.GetById;

internal sealed class GetSupplierByIdQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetSupplierByIdQuery, SupplierDetailResponse>
{
    public async Task<Result<SupplierDetailResponse>> Handle(GetSupplierByIdQuery query, CancellationToken cancellationToken)
    {
        var supplier = await context.Suppliers
            .Where(s => s.Id == query.Id)
            .Select(s => new { s.Id, s.Name, s.PrimaryPhone, s.SecondaryPhone, s.BankAccountNumber, s.Notes, s.IsActive, s.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (supplier is null)
        {
            return Result.Failure<SupplierDetailResponse>(SupplierErrors.NotFound(query.Id));
        }

        decimal goldBalance = await context.SupplierGoldLedgerEntries
            .Where(e => e.SupplierId == query.Id)
            .SumAsync(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Equivalent21KWeightInGrams : -e.Equivalent21KWeightInGrams, cancellationToken);

        decimal manufacturingBalance = await context.SupplierManufacturingLedgerEntries
            .Where(e => e.SupplierId == query.Id)
            .SumAsync(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Amount : -e.Amount, cancellationToken);

        List<SupplierFinancialTransaction> financialTransactions = await context.SupplierFinancialTransactions
            .Where(t => t.SupplierId == query.Id)
            .ToListAsync(cancellationToken);

        List<Guid> financialTxIds = financialTransactions.Select(t => t.Id).ToList();

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

        List<SupplierTransactionResponse> recentTransactions = await GetRecentTransactions(query.Id, financialTxIds, cancellationToken);

        return new SupplierDetailResponse
        {
            Id = supplier.Id,
            Name = supplier.Name,
            PrimaryPhone = supplier.PrimaryPhone,
            SecondaryPhone = supplier.SecondaryPhone,
            BankAccountNumber = supplier.BankAccountNumber,
            Notes = supplier.Notes,
            IsActive = supplier.IsActive,
            CreatedAt = supplier.CreatedAt,
            GoldBalance = goldBalance,
            ManufacturingBalance = manufacturingBalance,
            FinancialBalancesByCurrency = financialBalancesByCurrency,
            RecentTransactions = recentTransactions
        };
    }

    private async Task<List<SupplierTransactionResponse>> GetRecentTransactions(Guid supplierId, List<Guid> financialTxIds, CancellationToken cancellationToken)
    {
        List<SupplierTransactionResponse> goldEntries = await context.SupplierGoldLedgerEntries
            .Where(e => e.SupplierId == supplierId)
            .OrderByDescending(e => e.Date)
            .Take(5)
            .Select(e => new SupplierTransactionResponse
            {
                Id = e.Id,
                Description = e.ReferenceType == SupplierGoldReferenceType.SupplierDelivery ? "توريد ذهب" :
                              e.ReferenceType == SupplierGoldReferenceType.SupplierScrapPayment ? "استلام كسر ذهب" :
                              "تسوية يدوية",
                Date = e.Date,
                Type = "ذهب",
                Amount = e.Equivalent21KWeightInGrams,
                Unit = "جم",
                Direction = e.MovementType == SupplierBalanceMovementType.Increase ? "+" : "-"
            })
            .ToListAsync(cancellationToken);

        List<SupplierTransactionResponse> mfgEntries = await context.SupplierManufacturingLedgerEntries
            .Where(e => e.SupplierId == supplierId)
            .OrderByDescending(e => e.Date)
            .Take(5)
            .Select(e => new SupplierTransactionResponse
            {
                Id = e.Id,
                Description = e.ReferenceType == SupplierManufacturingReferenceType.SupplierDelivery ? "أجور تصنيع" :
                              e.ReferenceType == SupplierManufacturingReferenceType.SupplierManufacturingPayment ? "دفعة نقدية - أجور" :
                              "تسوية يدوية",
                Date = e.Date,
                Type = "تصنيع",
                Amount = e.Amount,
                Unit = "د.إ",
                Direction = e.MovementType == SupplierBalanceMovementType.Increase ? "+" : "-"
            })
            .ToListAsync(cancellationToken);

        List<SupplierTransactionResponse> financialEntries = await context.SupplierFinancialLedgerEntries
            .Where(e => financialTxIds.Contains(e.SupplierFinancialTransactionId))
            .OrderByDescending(e => e.Date)
            .Take(5)
            .Select(e => new SupplierTransactionResponse
            {
                Id = e.Id,
                Description = e.MovementType == SupplierBalanceMovementType.Increase ? "سلفة" : "دفعة سلفة",
                Date = e.Date,
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
