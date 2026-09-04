using Application.Abstractions.Messaging;
using Application.Common.Ledger;

namespace Application.Features.SupplierFinancialTransactions.Create;

public sealed record CreateSupplierFinancialTransactionCommand(
    Guid SupplierId,
    int Direction,
    decimal Amount,
    string Currency,
    Guid AccountId,
    DateTime Date,
    string? Notes,
    List<PaymentLegDto>? PaymentLegs) : ICommand<Guid>;
