using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Categories.Delete;

internal sealed class DeleteCategoryCommandHandler(IApplicationDbContext context)
    : ICommandHandler<DeleteCategoryCommand, Deleted>
{
    public async Task<Result<Deleted>> Handle(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        Category? category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (category is null)
        {
            return CategoryErrors.NotFound(command.Id);
        }

        bool hasChildren = await context.Categories
            .AnyAsync(c => c.ParentCategoryId == command.Id, cancellationToken);

        if (hasChildren)
        {
            return CategoryErrors.HasChildren;
        }

        bool hasReferences = await context.SalesInvoiceItems
            .AnyAsync(i => i.CategoryId == command.Id, cancellationToken)
            || await context.CustomerPurchaseInvoiceItems
                .AnyAsync(i => i.CategoryId == command.Id, cancellationToken);

        if (hasReferences)
        {
            return CategoryErrors.HasReferences;
        }

        context.Categories.Remove(category);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
