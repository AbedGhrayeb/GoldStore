using Application.Abstractions.Messaging;

namespace Application.SupplierDeliveries.Create;

public sealed record CreateSupplierDeliveryCommand(
    Guid SupplierId,
    List<DeliveryLineDto> Lines,
    decimal ManufacturingFeePerGram,
    string ManufacturingFeeCurrency,
    string? Notes)
    : ICommand<string>;
