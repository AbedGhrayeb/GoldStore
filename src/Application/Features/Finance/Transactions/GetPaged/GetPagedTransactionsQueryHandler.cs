using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Models;
using Domain.Common;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Finance.Transactions.GetPaged;

internal sealed class GetPagedTransactionsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetPagedTransactionsQuery, PaginatedList<RecentTransactionResponse>>
{
    public async Task<Result<PaginatedList<RecentTransactionResponse>>> Handle(
        GetPagedTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        //int page = Math.Max(query.Page, 1);
        //int pageSize = Math.Clamp(query.PageSize, 1, 100);

        IQueryable<FinancialTransaction> transactionsQuery = context.FinancialTransactions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Currency)
            && Enum.TryParse<Currency>(query.Currency, ignoreCase: true, out Currency currency))
        {
            transactionsQuery = transactionsQuery.Where(t => t.Currency == currency);
        }
        IQueryable<FinancialAccount> financialAccountsQuery = context.FinancialAccounts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.AccountType)
            && Enum.TryParse<FinancialAccountType>(query.AccountType, ignoreCase: true, out FinancialAccountType accountType))
        {
            List<Guid> matchingAccountIds = await financialAccountsQuery
                .Where(a => a.AccountType == accountType)
                .Select(a => a.Id)
                .Distinct()
                .ToListAsync(cancellationToken);

            transactionsQuery = transactionsQuery.Where(t => matchingAccountIds.Contains(t.AccountId));
        }

        if (!string.IsNullOrWhiteSpace(query.AccountName))
        {
            List<Guid> matchingAccountIds = await financialAccountsQuery
                .Where(a => a.Name.Contains(query.AccountName))
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);

            transactionsQuery = transactionsQuery.Where(t => matchingAccountIds.Contains(t.AccountId));
        }

        if (query.FromDate.HasValue)
        {
            var fromDate = DateTime.SpecifyKind(query.FromDate.Value, DateTimeKind.Utc);
            transactionsQuery = transactionsQuery.Where(t => t.CreatedAtUtc >= fromDate);
        }

        if (query.ToDate.HasValue)
        {
            var toDate = DateTime.SpecifyKind(query.ToDate.Value, DateTimeKind.Utc);
            transactionsQuery = transactionsQuery.Where(t => t.CreatedAtUtc <= toDate);
        }

        // int totalCount = await transactionsQuery.CountAsync(cancellationToken);

        List<Guid> allAccountIds = await transactionsQuery
            .Select(t => t.AccountId)
            .Distinct()
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> accountNames = await financialAccountsQuery
            .AsNoTracking()
            .Where(a => allAccountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);

        IQueryable<RecentTransactionResponse> items = transactionsQuery.Select(t => new RecentTransactionResponse
        {
            Id = t.Id,
            Date = t.CreatedAtUtc!.Value.LocalDateTime,
            Description = t.ReferenceType.GetDescription(),
            AccountName = accountNames.GetValueOrDefault(t.AccountId, string.Empty),
            Amount = t.Amount,
            Currency = t.Currency.ToString(),
            TransactionType = t.TransactionType.ToString(),
            ReferenceType = t.ReferenceType.ToString()
        });

        return await PaginatedList<RecentTransactionResponse>.CreateAsync(items, query.Page, query.PageSize);
    }

}
