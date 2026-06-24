using Application.Abstractions.Messaging;

namespace Application.Finance.Accounts.GetWithBalance;

public sealed record GetAccountsWithBalancesQuery(
    string? AccountType = null,
    bool ActiveOnly = true) : IQuery<List<AccountWithBalanceResponse>>;