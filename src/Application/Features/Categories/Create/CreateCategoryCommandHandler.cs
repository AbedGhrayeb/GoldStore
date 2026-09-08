// <copyright file="CreateCategoryCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Catalog;
using Domain.Common;
using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Categories.Create;

internal sealed class CreateCategoryCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CreateCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        bool nameExists = await context.Categories
            .AnyAsync(c => c.Name == command.Name && c.ParentCategoryId == command.ParentCategoryId, cancellationToken);

        if (nameExists)
        {
            return CategoryErrors.DuplicateName;
        }

        if (!Enum.IsDefined(typeof(Karat), command.Karat))
        {
            return Domain.Catalog.CategoryErrors.InvalidKarat;
        }

        var karat = (Karat)command.Karat;

        Result<Category> category = Category.Create(
            command.ParentCategoryId,
            command.Name,
            command.Description,
            command.WeightInGrams,
            karat);

        if (category.IsError)
        {
            return category.Errors;
        }

        context.Categories.Add(category.Value);

        // A new category's declared weight is opening stock: it increases the main
        // gold store weight in the category's karat bucket (same SaveChanges unit).
        Result<Updated> posted = await PostCategoryStockAsync(
            context,
            karat,
            command.WeightInGrams,
            GoldMovementType.Increase,
            $"رصيد افتتاحي للتصنيف {command.Name}",
            $"إنشاء تصنيف {command.Name} بوزن {command.WeightInGrams:N3} جم",
            cancellationToken);

        if (posted.IsError)
        {
            return posted.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        return category.Value.Id;
    }

    internal static async Task<Result<Updated>> PostCategoryStockAsync(
        IApplicationDbContext context,
        Karat karat,
        decimal weightInGrams,
        GoldMovementType movement,
        string reason,
        string? notes,
        CancellationToken cancellationToken)
    {
        var adjustmentType = movement == GoldMovementType.Increase
            ? InventoryAdjustmentType.Increase
            : InventoryAdjustmentType.Decrease;

        Result<InventoryAdjustment> adjustmentResult = InventoryAdjustment.Create(
            adjustmentType, karat, weightInGrams, reason, notes);

        if (adjustmentResult.IsError)
        {
            return adjustmentResult.Errors;
        }

        context.InventoryAdjustments.Add(adjustmentResult.Value);

        Result<GoldLedgerEntry> ledgerResult = GoldLedgerEntry.Create(
            karat,
            weightInGrams,
            movement,
            GoldReferenceType.InventoryAdjustment,
            adjustmentResult.Value.Id,
            notes);

        if (ledgerResult.IsError)
        {
            return ledgerResult.Errors;
        }

        context.GoldLedgerEntries.Add(ledgerResult.Value);

        return Result.Updated;
    }
}
