using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Finance.Transactions.GetPaged;

internal sealed class GetPagedTransactionsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetPagedTransactionsQuery, PagedTransactionResponse>
{
    public async Task<Result<PagedTransactionResponse>> Handle(
        GetPagedTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        IQueryable<FinancialTransaction> transactions = context.FinancialTransactions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Currency)
            && Enum.TryParse<Currency>(query.Currency, ignoreCase: true, out var currency))
        {
            transactions = transactions.Where(t => t.Currency == currency);
        }

        if (!string.IsNullOrWhiteSpace(query.AccountType)
            && Enum.TryParse<FinancialAccountType>(query.AccountType, ignoreCase: true, out var accountType))
        {
            List<Guid> matchingAccountIds = await context.FinancialAccounts
                .AsNoTracking()
                .Where(a => a.AccountType == accountType)
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);

            transactions = transactions.Where(t => matchingAccountIds.Contains(t.AccountId));
        }

        if (!string.IsNullOrWhiteSpace(query.AccountName))
        {
            List<Guid> matchingAccountIds = await context.FinancialAccounts
                .AsNoTracking()
                .Where(a => a.Name.Contains(query.AccountName))
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);

            transactions = transactions.Where(t => matchingAccountIds.Contains(t.AccountId));
        }

        if (query.FromDate.HasValue)
        {
            DateTime fromDate = DateTime.SpecifyKind(query.FromDate.Value, DateTimeKind.Utc);
            transactions = transactions.Where(t => t.Date >= fromDate);
        }

        if (query.ToDate.HasValue)
        {
            DateTime toDate = DateTime.SpecifyKind(query.ToDate.Value, DateTimeKind.Utc);
            transactions = transactions.Where(t => t.Date <= toDate);
        }

        int totalCount = await transactions.CountAsync(cancellationToken);

        List<Guid> allAccountIds = await transactions
            .OrderByDescending(t => t.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => t.AccountId)
            .Distinct()
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> accountNames = await context.FinancialAccounts
            .AsNoTracking()
            .Where(a => allAccountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);

        var pagedData = await transactions
            .OrderByDescending(t => t.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        List<RecentTransactionResponse> items = pagedData.Select(t => new RecentTransactionResponse
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

        return new PagedTransactionResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
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
            _ => referenceType.ToString()
        };
    }
}