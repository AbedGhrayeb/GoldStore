using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Domain.Expenses;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Expenses.Expenses.Create;

internal sealed class CreateExpenseCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CreateExpenseCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateExpenseCommand command, CancellationToken cancellationToken)
    {
        FinancialAccount? account = await context.FinancialAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);
        if (account is null)
        {
            return ExpenseErrors.AccountNotFound(command.AccountId);
        }

        if (!account.IsActive)
        {
            return ExpenseErrors.AccountInactive;
        }

        if (command.CategoryId.HasValue)
        {
            bool categoryExists = await context.ExpenseCategories
                .AsNoTracking()
                .AnyAsync(c => c.Id == command.CategoryId.Value && c.IsActive, cancellationToken);

            if (!categoryExists)
            {
                return ExpenseErrors.CategoryNotFound(command.CategoryId.Value);
            }
        }

        decimal availableBalance = await context.GetAccountBalanceAsync(command.AccountId, cancellationToken);

        if (command.Amount > availableBalance)
        {
            return FinancialAccountErrors.InsufficientBalance(availableBalance, command.Amount);
        }

        Result<Expense> expenseResult = Expense.Create(command.CategoryId, command.AccountId, command.Amount, command.Description, command.ExpenseDate);
        if (expenseResult.IsError)
        {
            return expenseResult.Errors;
        }

        context.Expenses.Add(expenseResult.Value);

        Result<FinancialTransaction> financialTransactionResult = FinancialTransaction.Create(
            command.AccountId,
            account.Currency,
            command.Amount,
            FinancialTransactionType.Outflow,
            FinancialReferenceType.Expense,
            expenseResult.Value.Id,
            command.Description);

        if (financialTransactionResult.IsError)
        {
            return financialTransactionResult.Errors;
        }

        context.FinancialTransactions.Add(financialTransactionResult.Value);
        await context.SaveChangesAsync(cancellationToken);

        return expenseResult.Value.Id;
    }
}
