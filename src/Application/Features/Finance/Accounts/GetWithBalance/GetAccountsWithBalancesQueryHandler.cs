using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Finance.Accounts.GetWithBalance;

internal sealed class GetAccountsWithBalancesQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetAccountsWithBalancesQuery, List<AccountWithBalanceResponse>>
{
    public async Task<Result<List<AccountWithBalanceResponse>>> Handle(
        GetAccountsWithBalancesQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<FinancialAccount> accounts = context.FinancialAccounts.AsNoTracking().Where(a => a.TenantId == currentTenant.TenantId);

        if (query.ActiveOnly)
        {
            accounts = accounts.Where(a => a.IsActive);
        }

        if (query.Currency.HasValue)
        {
            accounts = accounts.Where(a => a.Currency == query.Currency.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.AccountType)
            && Enum.TryParse<FinancialAccountType>(query.AccountType, ignoreCase: true, out FinancialAccountType accountType))
        {
            accounts = accounts.Where(a => a.AccountType == accountType);
        }

        List<FinancialAccount> accountList = await accounts
            .OrderBy(a => a.Currency)
            .ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);

        var accountIds = accountList.Select(a => a.Id).ToList();

        var balanceData = await context.FinancialTransactions
            .AsNoTracking()
            .Where(t => t.TenantId == currentTenant.TenantId && accountIds.Contains(t.AccountId))
            .GroupBy(t => t.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Balance = g.Sum(t => t.TransactionType == FinancialTransactionType.Inflow ? t.Amount : -t.Amount)
            })
            .ToListAsync(cancellationToken);

        var balances = balanceData
            .ToDictionary(x => x.AccountId, x => x.Balance);

        var allTransactions = await context.FinancialTransactions
            .AsNoTracking()
            .Where(t => t.TenantId == currentTenant.TenantId && accountIds.Contains(t.AccountId))
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new { t.AccountId, t.Amount, t.TransactionType })
            .ToListAsync(cancellationToken);

        Dictionary<Guid, (decimal Amount, FinancialTransactionType Direction)> lastChangeMap = allTransactions
            .GroupBy(t => t.AccountId)
            .ToDictionary(
                g => g.Key,
                g => (g.First().Amount, g.First().TransactionType));

        var result = accountList.Select(a =>
        {
            decimal balance = balances.GetValueOrDefault(a.Id, 0m);
            (decimal lastAmount, FinancialTransactionType lastType) = lastChangeMap.GetValueOrDefault(a.Id, (0m, FinancialTransactionType.Inflow));

            return new AccountWithBalanceResponse
            {
                Id = a.Id,
                Name = a.Name,
                Currency = a.Currency.ToString(),
                AccountType = a.AccountType.ToString(),
                AccountNumber = a.AccountNumber,
                IsActive = a.IsActive,
                Balance = balance,
                LastChangeAmount = lastAmount,
                LastChangeDirection = lastType == FinancialTransactionType.Inflow ? "Inflow" : "Outflow"
            };
        }).ToList();

        return result;
    }
}
