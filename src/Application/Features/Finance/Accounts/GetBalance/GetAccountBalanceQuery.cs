using Application.Abstractions.Messaging;

namespace Application.Finance.Accounts.GetBalance;

public sealed record GetAccountBalanceQuery(Guid AccountId) : IQuery<AccountBalanceResponse>;
