using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Expenses;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Expenses.Expenses.Delete;

internal sealed class DeleteExpenseCommandHandler(IApplicationDbContext context)
    : ICommandHandler<DeleteExpenseCommand, Deleted>
{
    public async Task<Result<Deleted>> Handle(DeleteExpenseCommand command, CancellationToken cancellationToken)
    {
        Expense? expense = await context.Expenses.FindAsync([command.Id], cancellationToken);
        if (expense is null)
        {
            return ExpenseErrors.NotFound(command.Id);
        }

        FinancialTransaction? transaction = await context.FinancialTransactions
            .FirstOrDefaultAsync(t => t.ReferenceType == FinancialReferenceType.Expense && t.ReferenceId == expense.Id, cancellationToken);

        if (transaction is not null)
        {
            context.FinancialTransactions.Remove(transaction);
        }

        context.Expenses.Remove(expense);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
