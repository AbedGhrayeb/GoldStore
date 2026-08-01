using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Catalog;
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

        Result<Category> category = Category.Create(command.ParentCategoryId, command.Name, command.Description);

        if (category.IsError)
        {
            return category.Errors;
        }
        context.Categories.Add(category.Value);
        await context.SaveChangesAsync(cancellationToken);

        return category.Value.Id;
    }
}
