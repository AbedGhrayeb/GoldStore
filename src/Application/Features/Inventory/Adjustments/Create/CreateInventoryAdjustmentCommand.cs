using Application.Abstractions.Messaging;
using Domain.Inventory;

namespace Application.Features.Inventory.Adjustments.Create;

public sealed record CreateInventoryAdjustmentCommand(
    int AdjustmentType,
    int Karat,
    decimal WeightInGrams,
    string Reason,
    string? Notes,
    DateTime Date) : ICommand<Guid>;