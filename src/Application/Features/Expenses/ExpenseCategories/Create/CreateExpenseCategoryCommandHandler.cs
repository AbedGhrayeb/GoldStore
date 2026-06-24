using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Expenses.ExpenseCategories.Create;

internal sealed class CreateExpenseCategoryCommandHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateExpenseCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateExpenseCategoryCommand command, CancellationToken cancellationToken)
    {
        bool nameExists = await context.ExpenseCategories
            .AsNoTracking()
            .AnyAsync(c => c.Name == command.Name, cancellationToken);

        if (nameExists)
        {
            return Result.Failure<Guid>(ExpenseCategoryErrors.DuplicateName);
        }

        var category = new ExpenseCategory
        {
            Id = Guid.CreateVersion7(),
            Name = command.Name,
            IsActive = true,
            CreatedAt = dateTimeProvider.UtcNow
        };

        context.ExpenseCategories.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}