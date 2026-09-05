// <copyright file="CreateDebtCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Domain.Common;
using Domain.Debts;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Finance.Debts.Create;

internal sealed class CreateDebtCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<CreateDebtCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateDebtCommand command, CancellationToken cancellationToken)
    {
        var direction = (DebtDirection)command.Direction;
        Currency currency = Enum.Parse<Currency>(command.Currency);

        FinancialAccount? account = await context.FinancialAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
        {
            return DebtErrors.AccountNotFound(command.AccountId);
        }

        if (account.Currency != currency)
        {
            return DebtErrors.AccountCurrencyMismatch;
        }

        Result<Debt> debtResult = Debt.Create(command.Name, command.Phone, direction, currency, command.AccountId, command.Notes);
        if (debtResult.IsError)
        {
            return debtResult.Errors;
        }

        context.Debts.Add(debtResult.Value);

        Result<DebtLedgerEntry> debtLedgerEntryResult = DebtLedgerEntry.Create(debtResult.Value.Id, command.Amount,
            DebtBalanceMovementType.Increase, $"إنشاء دين — {command.Name}");

        if (debtLedgerEntryResult.IsError)
        {
            return debtLedgerEntryResult.Errors;
        }

        context.DebtLedgerEntries.Add(debtLedgerEntryResult.Value);

        FinancialTransactionType financialTransactionType = direction switch
        {
            DebtDirection.Receivable => FinancialTransactionType.Outflow,
            DebtDirection.Payable => FinancialTransactionType.Inflow,
            _ => FinancialTransactionType.Outflow,
        };

        if (financialTransactionType == FinancialTransactionType.Outflow)
        {
            decimal availableBalance = await context.GetAccountBalanceAsync(command.AccountId, cancellationToken);

            if (command.Amount > availableBalance)
            {
                return FinancialAccountErrors.InsufficientBalance(availableBalance, command.Amount);
            }
        }

        Result<FinancialTransaction> financialTransactionResult = FinancialTransaction.Create(command.AccountId, currency, command.Amount,
            financialTransactionType, FinancialReferenceType.DebtCreation, debtResult.Value.Id, $"إنشاء {GetDirectionLabel(direction)}: {command.Name}");

        if (financialTransactionResult.IsError)
        {
            return financialTransactionResult.Errors;
        }

        context.FinancialTransactions.Add(financialTransactionResult.Value);

        await context.SaveChangesAsync(cancellationToken);

        return debtResult.Value.Id;
    }

    private static string GetDirectionLabel(DebtDirection direction) => direction switch
    {
        DebtDirection.Receivable => "لنا - ذمة مدينة",
        DebtDirection.Payable => "له - ذمة دائنة",
        _ => "دين",
    };
}
