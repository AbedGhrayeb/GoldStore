using Application.Abstractions.Messaging;

namespace Application.SupplierPayments.ScrapGold.Create;

public sealed record CreateSupplierScrapGoldPaymentCommand(
    Guid SupplierId,
    int Karat,
    decimal WeightInGrams,
    string? Notes)
    : ICommand<Guid>;
