using Application.Abstractions.Messaging;

namespace Application.Features.SupplierFinancialTransactions.Create;

public sealed record CreateSupplierFinancialTransactionCommand(
    Guid SupplierId,
    int Direction,
    decimal Amount,
    string Currency,
    Guid AccountId,
    DateTime Date,
    string? Notes) : ICommand<Guid>;
