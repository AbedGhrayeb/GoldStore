using Application.Abstractions.Messaging;

namespace Application.Features.Finance.Debts.Payments;

public sealed record CreatePaymentCommand(
    Guid DebtId,
    Guid AccountId,
    decimal Amount,
    DateTime Date,
    string? Notes) : ICommand<Guid>;
