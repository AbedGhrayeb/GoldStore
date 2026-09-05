// <copyright file="SetAccountBalanceCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Finance.Accounts.SetBalance;

internal sealed class SetAccountBalanceCommandHandler(IApplicationDbContext context)
    : ICommandHandler<SetAccountBalanceCommand, Updated>
{
    public async Task<Result<Updated>> Handle(SetAccountBalanceCommand command, CancellationToken cancellationToken)
    {
        if (command.TargetBalance < 0m)
        {
            return FinancialAccountErrors.InvalidTargetBalance;
        }

        FinancialAccount? account = await context.FinancialAccounts
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
        {
            return FinancialAccountErrors.NotFound(command.AccountId);
        }

        if (!account.IsActive)
        {
            return FinancialAccountErrors.Inactive;
        }

        decimal currentBalance = await context.FinancialTransactions
            .Where(t => t.AccountId == account.Id)
            .SumAsync(t => t.TransactionType == FinancialTransactionType.Inflow ? t.Amount : -t.Amount, cancellationToken);

        decimal diff = command.TargetBalance - currentBalance;

        if (diff == 0m)
        {
            return Result.Updated;
        }

        Result<FinancialTransaction> financialTransactionResult = FinancialTransaction.Create(account.Id, account.Currency, Math.Abs(diff),
                 diff > 0m ? FinancialTransactionType.Inflow : FinancialTransactionType.Outflow,
                 FinancialReferenceType.ManualAdjustment, account.Id, command.Notes);

        if (financialTransactionResult.IsError)
        {
            return financialTransactionResult.Errors;
        }

        context.FinancialTransactions.Add(financialTransactionResult.Value);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
