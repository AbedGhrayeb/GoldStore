using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Errors;
using Application.Common.Ledger;
using Domain.Debts;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Finance.Debts.Update;

internal sealed class UpdateDebtCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<UpdateDebtCommand, Updated>
{
    public async Task<Result<Updated>> Handle(UpdateDebtCommand command, CancellationToken cancellationToken)
    {
        Debt? debt = await context.Debts.FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (debt is null)
        { return DebtErrors.NotFound(command.Id); }
        try
        {
            // --- Determine target account ---
            Guid? targetAccountId = command.NewAccountId ?? debt.AccountId;
            // --- Update basic fields ---
            Result<Updated> debtUpdateResult = debt.Update(command.Name ?? debt.Name, command.Phone ?? debt.Phone,
                     targetAccountId, command.Notes ?? debt.Notes);

            if (debtUpdateResult.IsError)
            {
                return debtUpdateResult.Errors;
            }
            // --- Compute current ledger totals ---
            decimal totalIncrease = await context.DebtLedgerEntries
                .Where(e => e.DebtId == command.Id && e.MovementType == DebtBalanceMovementType.Increase)
                .SumAsync(e => e.Amount, cancellationToken);

            decimal totalDecrease = await context.DebtLedgerEntries
                .Where(e => e.DebtId == command.Id && e.MovementType == DebtBalanceMovementType.Decrease)
                .SumAsync(e => e.Amount, cancellationToken);

            decimal pendingOutflowsOnTargetAccount = 0m;

            bool accountChanging = targetAccountId.HasValue
                && targetAccountId != debt.AccountId;

            bool amountChanging = command.NewAmount.HasValue;

            // --- Handle account change ---
            if (accountChanging)
            {
                FinancialAccount? targetAccount = await context.FinancialAccounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == targetAccountId!.Value, cancellationToken);

                if (targetAccount is null)
                { return DebtErrors.AccountNotFound(targetAccountId!.Value); }

                if (targetAccount.Currency != debt.Currency)
                {
                    return DebtErrors.AccountCurrencyMismatch;
                }

                // Find all previous financial transactions for this debt
                List<FinancialTransaction> previousTransactions = await context.FinancialTransactions
                    .Where(t => t.ReferenceId == debt.Id
                        && (t.ReferenceType == FinancialReferenceType.DebtCreation
                            || t.ReferenceType == FinancialReferenceType.DebtAdjustment))
                    .ToListAsync(cancellationToken);

                if (previousTransactions.Count > 0)
                {
                    // Reverse each on old account
                    foreach (FinancialTransaction tx in previousTransactions)
                    {
                        Result<FinancialTransaction> newTransactionResult = FinancialTransaction.Create(
                            tx.AccountId,
                            tx.Currency,
                            tx.Amount,
                            tx.TransactionType == FinancialTransactionType.Inflow
                                ? FinancialTransactionType.Outflow
                                : FinancialTransactionType.Inflow,
                            FinancialReferenceType.DebtAdjustment,
                            debt.Id,
                            $"إلغاء نقل حساب — {debt.Name}"
                        );

                        context.FinancialTransactions.Add(newTransactionResult.Value);
                    }

                    // Re-apply on new account (same directions as original)
                    decimal reAppliedOutflowTotal = previousTransactions
                        .Where(t => t.TransactionType == FinancialTransactionType.Outflow)
                        .Sum(t => t.Amount);

                    if (reAppliedOutflowTotal > 0m)
                    {
                        decimal availableBalance = await context.GetAccountBalanceAsync(targetAccountId!.Value, cancellationToken);

                        if (reAppliedOutflowTotal > availableBalance)
                        {
                            return FinancialAccountErrors.InsufficientBalance(availableBalance, reAppliedOutflowTotal);
                        }
                    }

                    foreach (FinancialTransaction tx in previousTransactions)
                    {
                        Result<FinancialTransaction> newTransactionResult = FinancialTransaction.Create(
                             targetAccountId!.Value,
                            tx.Currency,
                            tx.Amount,
                            tx.TransactionType,
                            FinancialReferenceType.DebtAdjustment, debt.Id, $"نقل حساب — {debt.Name}");
                        context.FinancialTransactions.Add(newTransactionResult.Value);

                        if (tx.TransactionType == FinancialTransactionType.Outflow)
                        {
                            pendingOutflowsOnTargetAccount += tx.Amount;
                        }
                    }
                }
                else if (totalIncrease > 0)
                {
                    // Debt has balance but no financial transactions yet (legacy debt with no account)
                    // Create initial financial entries on the new account
                    decimal netBalance = totalIncrease - totalDecrease;
                    if (netBalance > 0)
                    {
                        FinancialTransactionType initialTxType = debt.Direction switch
                        {
                            DebtDirection.Receivable => FinancialTransactionType.Outflow,
                            DebtDirection.Payable => FinancialTransactionType.Inflow,
                            _ => FinancialTransactionType.Outflow
                        };

                        if (initialTxType == FinancialTransactionType.Outflow)
                        {
                            decimal availableBalance = await context.GetAccountBalanceAsync(targetAccountId!.Value, cancellationToken);

                            if (netBalance > availableBalance)
                            {
                                return FinancialAccountErrors.InsufficientBalance(availableBalance, netBalance);
                            }
                        }

                        context.FinancialTransactions.Add(FinancialTransaction.Create(targetAccountId!.Value,
                            targetAccount.Currency, netBalance, initialTxType, FinancialReferenceType.DebtAdjustment, debt.Id, $"نقل حساب — {debt.Name}").Value);

                    }
                }

            }

            // --- Handle amount change ---
            if (amountChanging)
            {
                decimal newAmount = command.NewAmount!.Value;

                if (newAmount < totalDecrease)

                {
                    return DebtErrors.AmountBelowPayments(newAmount, totalDecrease);
                }

                decimal diff = newAmount - totalIncrease;

                if (diff != 0)
                {

                    Result<DebtLedgerEntry> debtLedgerEntryResult = DebtLedgerEntry.Create(debt.Id, Math.Abs(diff),
                        diff > 0 ? DebtBalanceMovementType.Increase : DebtBalanceMovementType.Decrease,
                        $"تعديل قيمة الدين — {debt.Name}");


                    if (debtLedgerEntryResult.IsError)
                    {
                        return debtLedgerEntryResult.Errors;
                    }

                    context.DebtLedgerEntries.Add(debtLedgerEntryResult.Value);


                    // Financial transaction on the target account
                    Guid? effectiveAccountId = targetAccountId ?? debt.AccountId;

                    if (effectiveAccountId.HasValue)
                    {
                        FinancialAccount? account = await context.FinancialAccounts
                            .AsNoTracking()
                            .FirstOrDefaultAsync(a => a.Id == effectiveAccountId.Value, cancellationToken);

                        if (account is null)

                        {
                            return DebtErrors.AccountNotFound(effectiveAccountId.Value);
                        }

                        FinancialTransactionType txType = debt.Direction switch
                        {
                            DebtDirection.Receivable => diff > 0
                                ? FinancialTransactionType.Outflow
                                : FinancialTransactionType.Inflow,
                            DebtDirection.Payable => diff > 0
                                ? FinancialTransactionType.Inflow
                                : FinancialTransactionType.Outflow,
                            _ => FinancialTransactionType.Outflow
                        };

                        if (txType == FinancialTransactionType.Outflow)
                        {
                            decimal availableBalance = await context.GetAccountBalanceAsync(effectiveAccountId.Value, cancellationToken);

                            if (effectiveAccountId == targetAccountId)
                            {
                                availableBalance -= pendingOutflowsOnTargetAccount;
                            }

                            if (Math.Abs(diff) > availableBalance)
                            {
                                return FinancialAccountErrors.InsufficientBalance(availableBalance, Math.Abs(diff));
                            }
                        }

                        Result<FinancialTransaction> financialTransactionResult = FinancialTransaction.Create(effectiveAccountId.Value, account.Currency,
                                Math.Abs(diff), txType,
                                FinancialReferenceType.DebtAdjustment, debt.Id, $"تعديل قيمة الدين — {debt.Name}");


                        if (financialTransactionResult.IsError)
                        {
                            return financialTransactionResult.Errors;
                        }

                        context.FinancialTransactions.Add(financialTransactionResult.Value);

                    }
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return ApplicationErrors.DatabaseError(ex);
        }


        return Result.Updated;
    }
}
