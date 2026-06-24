using Application.Abstractions.Messaging;

namespace Application.Finance.Accounts.Create;

public sealed record CreateFinancialAccountCommand(
    string Name,
    string Currency,
    string? AccountNumber,
    string? Notes) : ICommand<Guid>;