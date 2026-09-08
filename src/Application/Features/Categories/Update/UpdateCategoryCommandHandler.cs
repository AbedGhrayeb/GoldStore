// <copyright file="UpdateCategoryCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Categories.Create;
using Application.Common.Ledger;
using Domain.Catalog;
using Domain.Common;
using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Categories.Update;

internal sealed class UpdateCategoryCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateCategoryCommand, Updated>
{
    public async Task<Result<Updated>> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        Category? category = await context.Categories.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (category is null)
        {
            return CategoryErrors.NotFound(command.Id);
        }

        bool nameExists = await context.Categories
            .AnyAsync(c => c.Name == command.Name && c.ParentCategoryId == command.ParentCategoryId && c.Id != command.Id, cancellationToken);

        if (nameExists)
        {
            return CategoryErrors.DuplicateName;
        }

        if (command.ParentCategoryId == command.Id)
        {
            return CategoryErrors.CircularReference;
        }

        if (!Enum.IsDefined(typeof(Karat), command.Karat))
        {
            return Domain.Catalog.CategoryErrors.InvalidKarat;
        }

        decimal oldWeight = category.WeightInGrams ?? 0M;
        Karat? oldKarat = category.Karat;
        var newKarat = (Karat)command.Karat;

        // A weight/karat change moves the main gold store weight: validate the stock
        // guards BEFORE mutating anything, so a blocked edit leaves no in-memory
        // changes behind either.
        List<(Karat Karat, decimal Weight, GoldMovementType Movement, string Notes)> postings = [];

        if (oldKarat.HasValue && oldKarat.Value == newKarat)
        {
            decimal delta = command.WeightInGrams - oldWeight;

            if (delta > 0)
            {
                postings.Add((newKarat, delta, GoldMovementType.Increase,
                    $"زيادة وزن التصنيف من {oldWeight:N3} إلى {command.WeightInGrams:N3} جم"));
            }
            else if (delta < 0)
            {
                decimal decrease = -delta;
                decimal available = await context.GetGoldStockAsync(newKarat, cancellationToken);

                if (decrease > available)
                {
                    return GoldInventoryErrors.InsufficientStock(newKarat, available, decrease);
                }

                postings.Add((newKarat, decrease, GoldMovementType.Decrease,
                    $"تخفيض وزن التصنيف من {oldWeight:N3} إلى {command.WeightInGrams:N3} جم"));
            }
        }
        else
        {
            if (oldKarat.HasValue && oldWeight > 0)
            {
                decimal available = await context.GetGoldStockAsync(oldKarat.Value, cancellationToken);

                if (oldWeight > available)
                {
                    return GoldInventoryErrors.InsufficientStock(oldKarat.Value, available, oldWeight);
                }

                postings.Add((oldKarat.Value, oldWeight, GoldMovementType.Decrease,
                    $"نقل وزن التصنيف {oldWeight:N3} جم من {oldKarat.Value.KaratLabel()}"));
            }

            postings.Add((newKarat, command.WeightInGrams, GoldMovementType.Increase,
                $"نقل وزن التصنيف {command.WeightInGrams:N3} جم إلى {newKarat.KaratLabel()}"));
        }

        Result<Updated> updatedCategory = category.Update(
            command.ParentCategoryId,
            command.Name,
            command.Description,
            command.IsActive,
            command.WeightInGrams,
            newKarat);

        if (updatedCategory.IsError)
        {
            return updatedCategory.Errors;
        }

        foreach ((Karat karat, decimal weight, GoldMovementType movement, string notes) in postings)
        {
            Result<Updated> posted = await CreateCategoryCommandHandler.PostCategoryStockAsync(
                context,
                karat,
                weight,
                movement,
                $"تعديل التصنيف {command.Name}",
                notes,
                cancellationToken);

            if (posted.IsError)
            {
                return posted.Errors;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
