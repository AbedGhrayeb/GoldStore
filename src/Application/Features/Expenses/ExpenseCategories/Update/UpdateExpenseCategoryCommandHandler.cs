using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Expenses.ExpenseCategories.Update;

internal sealed class UpdateExpenseCategoryCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateExpenseCategoryCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateExpenseCategoryCommand command, CancellationToken cancellationToken)
    {
        ExpenseCategory? category = await context.ExpenseCategories.FindAsync([command.Id], cancellationToken);

        if (category is null)
        {
            return Result.Failure<bool>(ExpenseCategoryErrors.NotFound(command.Id));
        }

        bool nameExists = await context.ExpenseCategories
            .AsNoTracking()
            .AnyAsync(c => c.Name == command.Name && c.Id != command.Id, cancellationToken);

        if (nameExists)
        {
            return Result.Failure<bool>(ExpenseCategoryErrors.DuplicateName);
        }

        category.Name = command.Name;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}