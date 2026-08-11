using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Expenses.ExpenseCategories.Delete;

internal sealed class DeleteExpenseCategoryCommandHandler(IApplicationDbContext context)
    : ICommandHandler<DeleteExpenseCategoryCommand, Deleted>
{
    public async Task<Result<Deleted>> Handle(DeleteExpenseCategoryCommand command, CancellationToken cancellationToken)
    {
        ExpenseCategory? category = await context.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (category is null)
        {
            return ExpenseCategoryErrors.NotFound(command.Id);
        }

        bool hasExpenses = await context.Expenses.AsNoTracking()
            .AnyAsync(e => e.ExpenseCategoryId == command.Id, cancellationToken);

        if (hasExpenses)
        {
            return ExpenseCategoryErrors.HasExpenses;
        }

        context.ExpenseCategories.Remove(category);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
