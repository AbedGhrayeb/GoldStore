using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Inventory;
using SharedKernel;

namespace Application.Features.Inventory.Adjustments.Create;

internal sealed class CreateInventoryAdjustmentCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<CreateInventoryAdjustmentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateInventoryAdjustmentCommand command, CancellationToken cancellationToken)
    {
        var adjustmentType = (InventoryAdjustmentType)command.AdjustmentType;
        var karat = (Karat)command.Karat;

        decimal equivalent21K = command.WeightInGrams > 0
            ? GoldWeight.CalculateEquivalent21KWeight(command.WeightInGrams, karat)
            : 0m;

        Guid userId = userContext.UserId;

        var adjustment = new InventoryAdjustment
        {
            Id = Guid.CreateVersion7(),
            Type = adjustmentType,
            Karat = karat,
            WeightInGrams = command.WeightInGrams,
            Equivalent21KWeightInGrams = equivalent21K,
            Reason = command.Reason,
            Notes = command.Notes,
            UserId = userId,
            Date = command.Date
        };

        context.InventoryAdjustments.Add(adjustment);

        if (command.WeightInGrams > 0)
        {
            var movementType = adjustmentType switch
            {
                InventoryAdjustmentType.Increase => GoldMovementType.Increase,
                InventoryAdjustmentType.Decrease => GoldMovementType.Decrease,
                InventoryAdjustmentType.Damage => GoldMovementType.Decrease,
                InventoryAdjustmentType.Loss => GoldMovementType.Decrease,
                InventoryAdjustmentType.Correction => GoldMovementType.Decrease,
                _ => GoldMovementType.Increase
            };

            context.GoldLedgerEntries.Add(new GoldLedgerEntry
            {
                Id = Guid.CreateVersion7(),
                Karat = karat,
                WeightInGrams = command.WeightInGrams,
                Equivalent21KWeightInGrams = equivalent21K,
                MovementType = movementType,
                ReferenceType = GoldReferenceType.InventoryAdjustment,
                ReferenceId = adjustment.Id,
                UserId = userId,
                Date = command.Date,
                Notes = $"{adjustmentType} — {command.Reason}"
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(adjustment.Id);
    }
}