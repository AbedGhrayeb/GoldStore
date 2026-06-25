using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Debts;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Finance.Debts.Update;

internal sealed class UpdateDebtCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<UpdateDebtCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UpdateDebtCommand command, CancellationToken cancellationToken)
    {
        Debt? debt = await context.Debts
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (debt is null)
            return Result.Failure<Guid>(DebtErrors.NotFound(command.Id));

        // --- Update basic fields ---
        if (command.Name is not null)
            debt.Name = command.Name;

        if (command.Phone is not null)
            debt.Phone = command.Phone;

        if (command.Notes is not null)
            debt.Notes = command.Notes;

        // --- Determine target account ---
        Guid? targetAccountId = command.NewAccountId ?? debt.AccountId;

        // --- Compute current ledger totals ---
        decimal totalIncrease = await context.DebtLedgerEntries
            .Where(e => e.DebtId == command.Id && e.MovementType == DebtBalanceMovementType.Increase)
            .SumAsync(e => e.Amount, cancellationToken);

        decimal totalDecrease = await context.DebtLedgerEntries
            .Where(e => e.DebtId == command.Id && e.MovementType == DebtBalanceMovementType.Decrease)
            .SumAsync(e => e.Amount, cancellationToken);

        bool accountChanging = targetAccountId.HasValue
            && targetAccountId != debt.AccountId;

        bool amountChanging = command.NewAmount.HasValue;

        // --- Handle account change ---
        if (accountChanging)
        {
            var targetAccount = await context.FinancialAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == targetAccountId!.Value, cancellationToken);

            if (targetAccount is null)
                return Result.Failure<Guid>(DebtErrors.AccountNotFound(targetAccountId!.Value));

            if (targetAccount.Currency != debt.Currency)
                return Result.Failure<Guid>(DebtErrors.AccountCurrencyMismatch);

            // Find all previous financial transactions for this debt
            var previousTransactions = await context.FinancialTransactions
                .Where(t => t.ReferenceId == command.Id
                    && (t.ReferenceType == FinancialReferenceType.DebtCreation
                        || t.ReferenceType == FinancialReferenceType.DebtAdjustment))
                .ToListAsync(cancellationToken);

            if (previousTransactions.Count > 0)
            {
                // Reverse each on old account
                foreach (var tx in previousTransactions)
                {
                    context.FinancialTransactions.Add(new FinancialTransaction
                    {
                        Id = Guid.NewGuid(),
                        AccountId = tx.AccountId,
                        Currency = tx.Currency,
                        Amount = tx.Amount,
                        TransactionType = tx.TransactionType == FinancialTransactionType.Inflow
                            ? FinancialTransactionType.Outflow
                            : FinancialTransactionType.Inflow,
                        ReferenceType = FinancialReferenceType.DebtAdjustment,
                        ReferenceId = command.Id,
                        Date = DateTime.UtcNow,
                        Notes = $"إلغاء نقل حساب — {debt.Name}"
                    });
                }

                // Re-apply on new account (same directions as original)
                foreach (var tx in previousTransactions)
                {
                    context.FinancialTransactions.Add(new FinancialTransaction
                    {
                        Id = Guid.NewGuid(),
                        AccountId = targetAccountId!.Value,
                        Currency = targetAccount.Currency,
                        Amount = tx.Amount,
                        TransactionType = tx.TransactionType,
                        ReferenceType = FinancialReferenceType.DebtAdjustment,
                        ReferenceId = command.Id,
                        Date = DateTime.UtcNow,
                        Notes = $"نقل حساب — {debt.Name}"
                    });
                }
            }
            else if (totalIncrease > 0)
            {
                // Debt has balance but no financial transactions yet (legacy debt with no account)
                // Create initial financial entries on the new account
                var netBalance = totalIncrease - totalDecrease;
                if (netBalance > 0)
                {
                    var initialTxType = debt.Direction switch
                    {
                        DebtDirection.Receivable => FinancialTransactionType.Outflow,
                        DebtDirection.Payable => FinancialTransactionType.Inflow,
                        _ => FinancialTransactionType.Outflow
                    };

                    context.FinancialTransactions.Add(new FinancialTransaction
                    {
                        Id = Guid.NewGuid(),
                        AccountId = targetAccountId!.Value,
                        Currency = targetAccount.Currency,
                        Amount = netBalance,
                        TransactionType = initialTxType,
                        ReferenceType = FinancialReferenceType.DebtAdjustment,
                        ReferenceId = command.Id,
                        Date = DateTime.UtcNow,
                        Notes = $"نقل حساب (رصيد سابق) — {debt.Name}"
                    });
                }
            }

            debt.AccountId = targetAccountId;
        }

        // --- Handle amount change ---
        if (amountChanging)
        {
            decimal newAmount = command.NewAmount!.Value;

            if (newAmount < totalDecrease)
                return Result.Failure<Guid>(DebtErrors.AmountBelowPayments(newAmount, totalDecrease));

            decimal diff = newAmount - totalIncrease;

            if (diff != 0)
            {
                context.DebtLedgerEntries.Add(new DebtLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    DebtId = command.Id,
                    Amount = Math.Abs(diff),
                    MovementType = diff > 0 ? DebtBalanceMovementType.Increase : DebtBalanceMovementType.Decrease,
                    Date = DateTime.UtcNow,
                    Notes = $"تعديل قيمة الدين — {debt.Name}"
                });

                // Financial transaction on the target account
                Guid? effectiveAccountId = targetAccountId ?? debt.AccountId;

                if (effectiveAccountId.HasValue)
                {
                    FinancialAccount? account = await context.FinancialAccounts
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == effectiveAccountId.Value, cancellationToken);

                    if (account is null)
                        return Result.Failure<Guid>(DebtErrors.AccountNotFound(effectiveAccountId.Value));

                    var txType = debt.Direction switch
                    {
                        DebtDirection.Receivable => diff > 0
                            ? FinancialTransactionType.Outflow
                            : FinancialTransactionType.Inflow,
                        DebtDirection.Payable => diff > 0
                            ? FinancialTransactionType.Inflow
                            : FinancialTransactionType.Outflow,
                        _ => FinancialTransactionType.Outflow
                    };

                    context.FinancialTransactions.Add(new FinancialTransaction
                    {
                        Id = Guid.NewGuid(),
                        AccountId = effectiveAccountId.Value,
                        Currency = account.Currency,
                        Amount = Math.Abs(diff),
                        TransactionType = txType,
                        ReferenceType = FinancialReferenceType.DebtAdjustment,
                        ReferenceId = command.Id,
                        Date = DateTime.UtcNow,
                        Notes = $"تعديل قيمة الدين — {debt.Name}"
                    });
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(debt.Id);
    }
}
