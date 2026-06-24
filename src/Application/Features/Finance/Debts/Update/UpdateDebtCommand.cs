using Application.Abstractions.Messaging;

namespace Application.Features.Finance.Debts.Update;

public sealed record UpdateDebtCommand(
    Guid Id,
    string? Name,
    string? Phone,
    string? Notes) : ICommand<Guid>;
