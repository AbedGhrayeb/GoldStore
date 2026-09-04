using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Features.Finance.Debts.Update;

public sealed record UpdateDebtCommand(
    Guid Id,
    string? Name,
    string? Phone,
    decimal? NewAmount,
    Guid? NewAccountId,
    string? Notes) : ICommand<Updated>;
