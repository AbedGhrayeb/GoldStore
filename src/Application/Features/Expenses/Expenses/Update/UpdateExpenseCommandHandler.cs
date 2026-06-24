using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Expenses;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Expenses.Expenses.Update;

internal sealed class UpdateExpenseCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateExpenseCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateExpenseCommand command, CancellationToken cancellationToken)
    {
        Expense? expense = await context.Expenses.FindAsync([command.Id], cancellationToken);
        if (expense is null)
        {
            return Result.Failure<bool>(ExpenseErrors.NotFound(command.Id));
        }

        FinancialAccount? account = await context.FinancialAccounts.FindAsync([command.AccountId], cancellationToken);
        if (account is null)
        {
            return Result.Failure<bool>(Error.NotFound("Finance.AccountNotFound", "حساب الدفع غير موجود"));
        }

        if (!account.IsActive)
        {
            return Result.Failure<bool>(Error.Problem("Finance.AccountInactive", "حساب الدفع غير نشط"));
        }

        if (command.CategoryId.HasValue)
        {
            bool categoryExists = await context.ExpenseCategories
                .AsNoTracking()
                .AnyAsync(c => c.Id == command.CategoryId.Value && c.IsActive, cancellationToken);
            if (!categoryExists)
            {
                return Result.Failure<bool>(Error.NotFound("ExpenseCategories.NotFound", "تصنيف المصروف غير موجود أو غير نشط"));
            }
        }

        FinancialTransaction? existingTransaction = await context.FinancialTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ReferenceType == FinancialReferenceType.Expense && t.ReferenceId == expense.Id, cancellationToken);

        if (existingTransaction is not null)
        {
            context.FinancialTransactions.Remove(await context.FinancialTransactions.FindAsync([existingTransaction.Id], cancellationToken)
                ?? throw new InvalidOperationException("Transaction not found"));
        }

        expense.ExpenseDate = command.ExpenseDate;
        expense.ExpenseCategoryId = command.CategoryId;
        expense.Description = command.Description;
        expense.Amount = command.Amount;
        expense.AccountId = command.AccountId;

        context.FinancialTransactions.Add(new FinancialTransaction
        {
            Id = Guid.CreateVersion7(),
            AccountId = command.AccountId,
            Currency = account.Currency,
            Amount = command.Amount,
            TransactionType = FinancialTransactionType.Outflow,
            ReferenceType = FinancialReferenceType.Expense,
            ReferenceId = expense.Id,
            Date = command.ExpenseDate.ToDateTime(TimeOnly.MinValue),
            Notes = command.Description
        });

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}