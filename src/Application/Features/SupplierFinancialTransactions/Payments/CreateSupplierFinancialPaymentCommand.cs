using Application.Abstractions.Messaging;
using Application.Common.Ledger;

namespace Application.Features.SupplierFinancialTransactions.Payments;

public sealed record CreateSupplierFinancialPaymentCommand(
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    DateTime Date,
    string? Notes,
    List<PaymentLegDto>? PaymentLegs) : ICommand<Guid>;
