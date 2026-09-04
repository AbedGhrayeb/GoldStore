using Application.Abstractions.Messaging;
using Domain.Common;

namespace Application.Finance.Accounts.GetWithBalance;

public sealed record GetAccountsWithBalancesQuery(
    string? AccountType = null,
    bool ActiveOnly = true,
    Currency? Currency = null) : IQuery<List<AccountWithBalanceResponse>>;
