using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Debts;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Finance.Debts.Payments;

internal sealed class CreatePaymentCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<CreatePaymentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreatePaymentCommand command, CancellationToken cancellationToken)
    {
        Debt? debt = await context.Debts
            .FirstOrDefaultAsync(d => d.Id == command.DebtId, cancellationToken);

        if (debt is null)
        {
            return Result.Failure<Guid>(DebtErrors.NotFound(command.DebtId));
        }

        if (command.Amount <= 0)
        {
            return Result.Failure<Guid>(DebtErrors.PaymentAmountMustBePositive);
        }

        decimal paidSoFar = await context.DebtLedgerEntries
            .AsNoTracking()
            .Where(e => e.DebtId == command.DebtId)
            .SumAsync(e => e.MovementType == DebtBalanceMovementType.Increase
                ? e.Amount
                : -e.Amount, cancellationToken);

        if (command.Amount > paidSoFar)
        {
            return Result.Failure<Guid>(DebtErrors.PaymentExceedsBalance(command.Amount, paidSoFar));
        }

        var paymentId = Guid.CreateVersion7();

        context.DebtLedgerEntries.Add(new DebtLedgerEntry
        {
            Id = paymentId,
            DebtId = command.DebtId,
            Amount = command.Amount,
            MovementType = DebtBalanceMovementType.Decrease,
            Date = command.Date,
            Notes = command.Notes ?? $"دفعة على {debt.Name}"
        });

        var financialTransactionType = debt.Direction switch
        {
            DebtDirection.Receivable => FinancialTransactionType.Inflow,
            DebtDirection.Payable => FinancialTransactionType.Outflow,
            _ => FinancialTransactionType.Inflow
        };

        context.FinancialTransactions.Add(new FinancialTransaction
        {
            Id = Guid.CreateVersion7(),
            AccountId = command.AccountId,
            Currency = debt.Currency,
            Amount = command.Amount,
            TransactionType = financialTransactionType,
            ReferenceType = FinancialReferenceType.DebtPayment,
            ReferenceId = command.DebtId,
            Date = command.Date,
            Notes = command.Notes ?? $"دفعة على {GetDirectionLabel(debt.Direction)}: {debt.Name}"
        });

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(paymentId);
    }

    private static string GetDirectionLabel(DebtDirection direction) => direction switch
    {
        DebtDirection.Receivable => "ذمة مدينة",
        DebtDirection.Payable => "ذمة دائنة",
        _ => "دين"
    };
}
