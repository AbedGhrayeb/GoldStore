using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Categories.Update;

internal sealed class UpdateCategoryCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateCategoryCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        Category? category = await context.Categories.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (category is null)
        {
            return Result.Failure<bool>(CategoryErrors.NotFound(command.Id));
        }

        bool nameExists = await context.Categories
            .AnyAsync(c => c.Name == command.Name && c.ParentCategoryId == command.ParentCategoryId && c.Id != command.Id, cancellationToken);

        if (nameExists)
        {
            return Result.Failure<bool>(CategoryErrors.DuplicateName);
        }

        if (command.ParentCategoryId == command.Id)
        {
            return Result.Failure<bool>(Error.Failure("Categories.CircularReference", "A category cannot be its own parent."));
        }

        category.Name = command.Name;
        category.Description = command.Description;
        category.ParentCategoryId = command.ParentCategoryId;
        category.IsActive = command.IsActive;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}