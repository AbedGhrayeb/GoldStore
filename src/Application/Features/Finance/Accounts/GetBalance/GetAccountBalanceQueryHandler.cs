using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Finance.Accounts.GetBalance;

internal sealed class GetAccountBalanceQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetAccountBalanceQuery, AccountBalanceResponse>
{
    public async Task<Result<AccountBalanceResponse>> Handle(
        GetAccountBalanceQuery query,
        CancellationToken cancellationToken)
    {
        FinancialAccount? account = await context.FinancialAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == query.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure<AccountBalanceResponse>(FinancialAccountErrors.NotFound(query.AccountId));
        }

        decimal currentBalance = await context.FinancialTransactions
            .AsNoTracking()
            .Where(t => t.AccountId == account.Id)
            .SumAsync(t => t.TransactionType == FinancialTransactionType.Inflow ? t.Amount : -t.Amount, cancellationToken);

        return new AccountBalanceResponse
        {
            Id = account.Id,
            Name = account.Name,
            Currency = account.Currency.ToString(),
            AccountType = account.AccountType.ToString(),
            CurrentBalance = currentBalance
        };
    }
}
