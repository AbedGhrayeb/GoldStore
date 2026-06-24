using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Expenses;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Expenses.Expenses.Create;

internal sealed class CreateExpenseCommandHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateExpenseCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateExpenseCommand command, CancellationToken cancellationToken)
    {
        FinancialAccount? account = await context.FinancialAccounts.FindAsync([command.AccountId], cancellationToken);
        if (account is null)
        {
            return Result.Failure<Guid>(Error.NotFound("Finance.AccountNotFound", "حساب الدفع غير موجود"));
        }

        if (!account.IsActive)
        {
            return Result.Failure<Guid>(Error.Problem("Finance.AccountInactive", "حساب الدفع غير نشط"));
        }

        if (command.CategoryId.HasValue)
        {
            bool categoryExists = await context.ExpenseCategories
                .AsNoTracking()
                .AnyAsync(c => c.Id == command.CategoryId.Value && c.IsActive, cancellationToken);

            if (!categoryExists)
            {
                return Result.Failure<Guid>(Error.NotFound("ExpenseCategories.NotFound", "تصنيف المصروف غير موجود أو غير نشط"));
            }
        }

        var expense = new Expense
        {
            Id = Guid.CreateVersion7(),
            ExpenseCategoryId = command.CategoryId,
            AccountId = command.AccountId,
            Amount = command.Amount,
            Description = command.Description,
            ExpenseDate = command.ExpenseDate,
            CreatedAt = dateTimeProvider.UtcNow
        };

        context.Expenses.Add(expense);

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

        return expense.Id;
    }
}