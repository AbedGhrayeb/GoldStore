using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

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
            return Result.Failure<Guid>(CategoryErrors.DuplicateName);
        }

        var category = new Category
        {
            Id = Guid.CreateVersion7(),
            Name = command.Name,
            Description = command.Description,
            ParentCategoryId = command.ParentCategoryId,
            IsActive = command.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
