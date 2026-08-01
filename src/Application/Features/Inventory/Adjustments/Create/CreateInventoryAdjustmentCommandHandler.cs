using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Inventory;
using SharedKernel.Result;

namespace Application.Features.Inventory.Adjustments.Create;

internal sealed class CreateInventoryAdjustmentCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<CreateInventoryAdjustmentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateInventoryAdjustmentCommand command, CancellationToken cancellationToken)
    {
        var adjustmentType = (InventoryAdjustmentType)command.AdjustmentType;
        var karat = (Karat)command.Karat;


        Result<InventoryAdjustment> adjustmentResult = InventoryAdjustment.Create(adjustmentType, karat, command.WeightInGrams, command.Reason, command.Notes);
        if (adjustmentResult.IsError)
        {
            return adjustmentResult.Errors;
        }

        context.InventoryAdjustments.Add(adjustmentResult.Value);

        if (command.WeightInGrams > 0)
        {
            GoldMovementType movementType = adjustmentType switch
            {
                InventoryAdjustmentType.Increase => GoldMovementType.Increase,
                InventoryAdjustmentType.Decrease => GoldMovementType.Decrease,
                InventoryAdjustmentType.Damage => GoldMovementType.Decrease,
                InventoryAdjustmentType.Loss => GoldMovementType.Decrease,
                InventoryAdjustmentType.Correction => GoldMovementType.Decrease,
                _ => GoldMovementType.Increase
            };

            Result<GoldLedgerEntry> goldLedgerEntryResult = GoldLedgerEntry.Create(
                karat,
                command.WeightInGrams,
                movementType,
                GoldReferenceType.InventoryAdjustment,
                adjustmentResult.Value.Id,
                $"{adjustmentType} — {command.Reason}");

            if (goldLedgerEntryResult.IsError)
            {
                return goldLedgerEntryResult.Errors;
            }
            context.GoldLedgerEntries.Add(goldLedgerEntryResult.Value);
        }

        await context.SaveChangesAsync(cancellationToken);

        return adjustmentResult.Value.Id;
    }
}
