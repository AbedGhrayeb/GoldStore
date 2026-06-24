using Application.Abstractions.Messaging;

namespace Application.Finance.Accounts.SetBalance;

public sealed record SetAccountBalanceCommand(
    Guid AccountId,
    decimal TargetBalance,
    string? Notes) : ICommand;
