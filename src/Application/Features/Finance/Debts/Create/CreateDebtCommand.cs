using Application.Abstractions.Messaging;

namespace Application.Features.Finance.Debts.Create;

public sealed record CreateDebtCommand(
    string Name,
    string? Phone,
    int Direction,
    string Currency,
    Guid? AccountId,
    decimal Amount,
    string? Notes,
    DateTime Date) : ICommand<Guid>;
