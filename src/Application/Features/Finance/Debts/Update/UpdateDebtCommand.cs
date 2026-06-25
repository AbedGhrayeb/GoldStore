using Application.Abstractions.Messaging;

namespace Application.Features.Finance.Debts.Update;

public sealed record UpdateDebtCommand(
    Guid Id,
    string? Name,
    string? Phone,
    decimal? NewAmount,
    Guid? NewAccountId,
    string? Notes) : ICommand<Guid>;
