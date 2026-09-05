// <copyright file="UpdateCategoryCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Catalog;
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

        Result<Updated> updatedCategory = category.Update(command.ParentCategoryId, command.Name, command.Description, command.IsActive);

        if (updatedCategory.IsError)
        {
            return updatedCategory.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
