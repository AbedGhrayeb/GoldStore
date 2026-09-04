using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Expenses.ExpenseCategories.Create;

internal sealed class CreateExpenseCategoryCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CreateExpenseCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateExpenseCategoryCommand command, CancellationToken cancellationToken)
    {
        bool nameExists = await context.ExpenseCategories
            .AsNoTracking()
            .AnyAsync(c => c.Name == command.Name, cancellationToken);

        if (nameExists)
        {
            return ExpenseCategoryErrors.DuplicateName;
        }

        Result<ExpenseCategory> category = ExpenseCategory.Create(command.Name);
        if (category.IsError)
        {
            return category.Errors;
        }

        context.ExpenseCategories.Add(category.Value);
        await context.SaveChangesAsync(cancellationToken);

        return category.Value.Id;
    }
}
