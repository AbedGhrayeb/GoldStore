using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Categories.ToggleActive;

internal sealed class ToggleActiveCategoryCommandHandler(IApplicationDbContext context)
    : ICommandHandler<ToggleActiveCategoryCommand, bool>
{
    public async Task<Result<bool>> Handle(ToggleActiveCategoryCommand command, CancellationToken cancellationToken)
    {
        Category? category = await context.Categories.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (category is null)
        {
            return Result.Failure<bool>(CategoryErrors.NotFound(command.Id));
        }

        if (category.IsActive && await context.Categories.AnyAsync(c => c.ParentCategoryId == command.Id && c.IsActive, cancellationToken))
        {
            return Result.Failure<bool>(CategoryErrors.HasChildren);
        }

        category.IsActive = !category.IsActive;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}