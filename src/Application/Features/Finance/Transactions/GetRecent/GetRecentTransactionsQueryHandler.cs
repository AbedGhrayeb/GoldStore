using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Finance.Transactions.GetRecent;

internal sealed class GetRecentTransactionsQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetRecentTransactionsQuery, List<RecentTransactionResponse>>
{
    public async Task<Result<List<RecentTransactionResponse>>> Handle(
        GetRecentTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        int count = Math.Clamp(query.Count, 1, 100);

        IQueryable<FinancialTransaction> transactionsQuery = context.FinancialTransactions.OrderByDescending(c=>c.CreatedAtUtc).AsNoTracking().Where(t => t.TenantId == currentTenant.TenantId);

        var accountIds = transactionsQuery.Select(t => t.AccountId).Distinct().ToList();

        Dictionary<Guid, string> accountNames = await context.FinancialAccounts
            .AsNoTracking()
            .Where(a => a.TenantId == currentTenant.TenantId && accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);

        return await transactionsQuery.Select(t => new RecentTransactionResponse
        {
            Id = t.Id,
            Date =t.CreatedAtUtc.HasValue? t.CreatedAtUtc!.Value.LocalDateTime: default,
            Description = t.ReferenceType.GetDescription(),
            AccountName = accountNames.GetValueOrDefault(t.AccountId, string.Empty),
            Amount = t.Amount,
            Currency = t.Currency.ToString(),
            TransactionType = t.TransactionType.ToString(),
            ReferenceType = t.ReferenceType.ToString()
        })
            .Take(count)
            .ToListAsync();

    }
}
