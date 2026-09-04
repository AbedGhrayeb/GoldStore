using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Finance.Accounts.SetBalance;

public sealed record SetAccountBalanceCommand(
    Guid AccountId,
    decimal TargetBalance,
    string? Notes) : ICommand<Updated>;
