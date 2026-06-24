using Application.Abstractions.Messaging;

namespace Application.SupplierPayments.Manufacturing.Create;

public sealed record CreateSupplierManufacturingPaymentCommand(
    Guid SupplierId,
    Guid AccountId,
    decimal Amount,
    string Currency,
    string? Notes)
    : ICommand<Guid>;