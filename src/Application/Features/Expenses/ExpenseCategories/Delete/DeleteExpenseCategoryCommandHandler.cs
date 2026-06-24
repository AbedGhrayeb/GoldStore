using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Expenses.ExpenseCategories.Delete;

internal sealed class DeleteExpenseCategoryCommandHandler(IApplicationDbContext context)
    : ICommandHandler<DeleteExpenseCategoryCommand, bool>
{
    public async Task<Result<bool>> Handle(DeleteExpenseCategoryCommand command, CancellationToken cancellationToken)
    {
        ExpenseCategory? category = await context.ExpenseCategories.FindAsync([command.Id], cancellationToken);

        if (category is null)
        {
            return Result.Failure<bool>(ExpenseCategoryErrors.NotFound(command.Id));
        }

        bool hasExpenses = await context.Expenses.AsNoTracking()
            .AnyAsync(e => e.ExpenseCategoryId == command.Id, cancellationToken);

        if (hasExpenses)
        {
            return Result.Failure<bool>(ExpenseCategoryErrors.HasExpenses);
        }

        context.ExpenseCategories.Remove(category);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}