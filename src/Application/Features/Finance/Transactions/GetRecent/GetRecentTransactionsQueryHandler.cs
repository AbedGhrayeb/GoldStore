using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Finance.Transactions.GetRecent;

internal sealed class GetRecentTransactionsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetRecentTransactionsQuery, List<RecentTransactionResponse>>
{
    public async Task<Result<List<RecentTransactionResponse>>> Handle(
        GetRecentTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        int count = Math.Clamp(query.Count, 1, 100);

        var transactions = await context.FinancialTransactions
            .AsNoTracking()
            .OrderByDescending(t => t.Date)
            .Take(count)
            .Select(t => new
            {
                t.Id,
                t.Date,
                t.Notes,
                t.AccountId,
                t.Amount,
                t.Currency,
                t.TransactionType,
                t.ReferenceType
            })
            .ToListAsync(cancellationToken);

        List<Guid> accountIds = transactions.Select(t => t.AccountId).Distinct().ToList();

        Dictionary<Guid, string> accountNames = await context.FinancialAccounts
            .AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);

        var result = transactions.Select(t => new RecentTransactionResponse
        {
            Id = t.Id,
            Date = t.Date,
            Description = GetDescription(t.ReferenceType, t.Notes),
            AccountName = accountNames.GetValueOrDefault(t.AccountId, string.Empty),
            Amount = t.Amount,
            Currency = t.Currency.ToString(),
            TransactionType = t.TransactionType.ToString(),
            ReferenceType = t.ReferenceType.ToString()
        }).ToList();

        return result;
    }

    private static string GetDescription(FinancialReferenceType referenceType, string? notes)
    {
        if (!string.IsNullOrWhiteSpace(notes))
        {
            return notes;
        }

        return referenceType switch
        {
            FinancialReferenceType.SupplierManufacturingPayment => "دفعة أجور تصنيع",
            FinancialReferenceType.Expense => "مصروف",
            FinancialReferenceType.SalaryPayment => "دفعة راتب",
            FinancialReferenceType.SalesPayment => "دفعة مبيعات",
            FinancialReferenceType.CustomerGoldPurchase => "شراء ذهب",
            FinancialReferenceType.ManualAdjustment => "تسوية يدوية",
            FinancialReferenceType.DebtCreation => "إنشاء دين",
            FinancialReferenceType.DebtPayment => "دفعة دين",
            FinancialReferenceType.DebtAdjustment => "تعديل دين",
            _ => referenceType.ToString()
        };
    }
}