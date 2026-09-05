// <copyright file="ToggleActiveCategoryCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Categories.ToggleActive;

internal sealed class ToggleActiveCategoryCommandHandler(IApplicationDbContext context)
    : ICommandHandler<ToggleActiveCategoryCommand, Updated>
{
    public async Task<Result<Updated>> Handle(ToggleActiveCategoryCommand command, CancellationToken cancellationToken)
    {
        Category? category = await context.Categories.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (category is null)
        {
            return CategoryErrors.NotFound(command.Id);
        }

        if (category.IsActive && await context.Categories.AnyAsync(c => c.ParentCategoryId == command.Id && c.IsActive, cancellationToken))
        {
            return CategoryErrors.HasChildren;
        }

        category.IsActive = !category.IsActive;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
