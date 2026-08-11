using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Expenses.ExpenseCategories.Update;

internal sealed class UpdateExpenseCategoryCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateExpenseCategoryCommand, Updated>
{
    public async Task<Result<Updated>> Handle(UpdateExpenseCategoryCommand command, CancellationToken cancellationToken)
    {
        ExpenseCategory? category = await context.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (category is null)
        {
            return ExpenseCategoryErrors.NotFound(command.Id);
        }

        bool nameExists = await context.ExpenseCategories
            .AsNoTracking()
            .AnyAsync(c => c.Name == command.Name && c.Id != command.Id, cancellationToken);

        if (nameExists)
        {
            return ExpenseCategoryErrors.DuplicateName;
        }
        Result<Updated> updateResult = category.Update(command.Name);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }
        await context.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
