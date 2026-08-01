using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Debts;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Finance.Debts.Payments;

internal sealed class CreatePaymentCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<CreatePaymentCommand, Updated>
{
    public async Task<Result<Updated>> Handle(CreatePaymentCommand command, CancellationToken cancellationToken)
    {
        Debt? debt = await context.Debts
            .FirstOrDefaultAsync(d => d.Id == command.DebtId, cancellationToken);

        if (debt is null)
        {
            return DebtErrors.NotFound(command.DebtId);
        }

        FinancialAccount? account = await context.FinancialAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
        {
            return DebtErrors.AccountNotFound(command.AccountId);
        }

        if (account.Currency != debt.Currency)
        {
            return DebtErrors.AccountCurrencyMismatch;
        }

        if (command.Amount <= 0)
        {
            return DebtErrors.PaymentAmountMustBePositive;
        }

        decimal paidSoFar = await context.DebtLedgerEntries
            .AsNoTracking()
            .Where(e => e.DebtId == command.DebtId)
            .SumAsync(e => e.MovementType == DebtBalanceMovementType.Increase
                ? e.Amount
                : -e.Amount, cancellationToken);

        if (command.Amount > paidSoFar)
        {
            return DebtErrors.PaymentExceedsBalance(command.Amount, paidSoFar);
        }

        var paymentId = Guid.CreateVersion7();
        Result<DebtLedgerEntry> debtLedgerEntryResult = DebtLedgerEntry.Create(command.DebtId, command.Amount,
            DebtBalanceMovementType.Decrease, command.Notes ?? $"دفعة على {debt.Name}");
        if (debtLedgerEntryResult.IsError)
        {
            return debtLedgerEntryResult.Errors;
        }
        context.DebtLedgerEntries.Add(debtLedgerEntryResult.Value);

        Result<FinancialTransaction> financialTransactionResult = FinancialTransaction.Create(
            command.AccountId, account.Currency,
            command.Amount,
            debt.Direction switch
            {
                DebtDirection.Receivable => FinancialTransactionType.Inflow,
                DebtDirection.Payable => FinancialTransactionType.Outflow,
                _ => FinancialTransactionType.Inflow
            }, FinancialReferenceType.DebtPayment,
            command.DebtId,
            command.Notes ?? $"دفعة على {GetDirectionLabel(debt.Direction)}: {debt.Name}");

        if (financialTransactionResult.IsError)
        {
            return financialTransactionResult.Errors;
        }

        context.FinancialTransactions.Add(financialTransactionResult.Value);




        await context.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    private static string GetDirectionLabel(DebtDirection direction) => direction switch
    {
        DebtDirection.Receivable => "لنا - ذمة مدينة",
        DebtDirection.Payable => "علينا - ذمة دائنة",
        _ => "دين"
    };
}
