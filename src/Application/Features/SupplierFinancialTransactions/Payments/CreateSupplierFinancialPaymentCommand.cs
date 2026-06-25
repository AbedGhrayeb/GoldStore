using Application.Abstractions.Messaging;

namespace Application.Features.SupplierFinancialTransactions.Payments;

public sealed record CreateSupplierFinancialPaymentCommand(
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    DateTime Date,
    string? Notes) : ICommand<Guid>;
