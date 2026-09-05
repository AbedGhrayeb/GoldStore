// <copyright file="UpdateExpenseCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Domain.Expenses;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Expenses.Expenses.Update;

internal sealed class UpdateExpenseCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateExpenseCommand, Updated>
{
    public async Task<Result<Updated>> Handle(UpdateExpenseCommand command, CancellationToken cancellationToken)
    {
        Expense? expense = await context.Expenses
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);
        if (expense is null)
        {
            return ExpenseErrors.NotFound(command.Id);
        }

        FinancialAccount? account = await context.FinancialAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);
        if (account is null)
        {
            return Error.NotFound("Finance.AccountNotFound", "حساب الدفع غير موجود");
        }

        if (!account.IsActive)
        {
            return Error.Failure("Finance.AccountInactive", "حساب الدفع غير نشط");
        }

        if (command.CategoryId.HasValue)
        {
            bool categoryExists = await context.ExpenseCategories
                .AsNoTracking()
                .AnyAsync(c => c.Id == command.CategoryId.Value && c.IsActive, cancellationToken);
            if (!categoryExists)
            {
                return Error.NotFound("ExpenseCategories.NotFound", "تصنيف المصروف غير موجود أو غير نشط");
            }
        }

        Result<Updated> expenseUpdateResult = expense.Update(command.Id, command.CategoryId, command.AccountId, command.Amount, command.Description, command.ExpenseDate);

        if (expenseUpdateResult.IsError)
        {
            return expenseUpdateResult.Errors;
        }

        decimal availableBalance = await context.GetAccountBalanceExcludingAsync(command.AccountId, FinancialReferenceType.Expense, expense.Id, cancellationToken);

        if (command.Amount > availableBalance)
        {
            return FinancialAccountErrors.InsufficientBalance(availableBalance, command.Amount);
        }

        FinancialTransaction? existingTransaction = await context.FinancialTransactions
            .FirstOrDefaultAsync(
                t => t.ReferenceType == FinancialReferenceType.Expense && t.ReferenceId == expense.Id,
                cancellationToken);

        if (existingTransaction is not null)
        {
            Result<Updated> financialTransactionsUpdateResult = existingTransaction.Update(existingTransaction.Id, command.AccountId, account.Currency, command.Amount, FinancialTransactionType.Outflow,
            FinancialReferenceType.Expense, expense.Id, command.Description);
            if (financialTransactionsUpdateResult.IsError)
            {
                return financialTransactionsUpdateResult.Errors;
            }
        }
        else
        {
            Result<FinancialTransaction> financialTransactionsResult = FinancialTransaction.Create(command.AccountId, account.Currency, command.Amount,
                     FinancialTransactionType.Outflow, FinancialReferenceType.Expense, expense.Id, command.Description);
            if (financialTransactionsResult.IsError)
            {
                return financialTransactionsResult.Errors;
            }

            context.FinancialTransactions.Add(financialTransactionsResult.Value);
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
