using Application.Abstractions.Messaging;
using Application.Common.Ledger;

namespace Application.SupplierPayments.Manufacturing.Create;

public sealed record CreateSupplierManufacturingPaymentCommand(
    Guid SupplierId,
    Guid AccountId,
    decimal Amount,
    string Currency,
    string? Notes,
    List<PaymentLegDto>? PaymentLegs)
    : ICommand<Guid>;