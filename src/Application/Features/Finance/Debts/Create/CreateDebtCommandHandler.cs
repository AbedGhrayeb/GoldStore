using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Debts;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Finance.Debts.Create;

internal sealed class CreateDebtCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<CreateDebtCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateDebtCommand command, CancellationToken cancellationToken)
    {
        var direction = (DebtDirection)command.Direction;
        var currency = Enum.Parse<Currency>(command.Currency);

        FinancialAccount? account = await context.FinancialAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure<Guid>(DebtErrors.AccountNotFound(command.AccountId));
        }

        if (account.Currency != currency)
        {
            return Result.Failure<Guid>(DebtErrors.AccountCurrencyMismatch);
        }

        var debtId = Guid.CreateVersion7();

        var debt = new Debt
        {
            Id = debtId,
            Name = command.Name,
            Phone = command.Phone,
            Direction = direction,
            Currency = currency,
            AccountId = command.AccountId,
            Notes = command.Notes,
            CreatedAt = command.Date
        };

        context.Debts.Add(debt);

        context.DebtLedgerEntries.Add(new DebtLedgerEntry
        {
            Id = Guid.CreateVersion7(),
            DebtId = debtId,
            Amount = command.Amount,
            MovementType = DebtBalanceMovementType.Increase,
            Date = command.Date,
            Notes = $"إنشاء دين — {command.Name}"
        });

        var financialTransactionType = direction switch
        {
            DebtDirection.Receivable => FinancialTransactionType.Outflow,
            DebtDirection.Payable => FinancialTransactionType.Inflow,
            _ => FinancialTransactionType.Outflow
        };

        context.FinancialTransactions.Add(new FinancialTransaction
        {
            Id = Guid.CreateVersion7(),
            AccountId = command.AccountId,
            Currency = account.Currency,
            Amount = command.Amount,
            TransactionType = financialTransactionType,
            ReferenceType = FinancialReferenceType.DebtCreation,
            ReferenceId = debtId,
            Date = command.Date,
            Notes = $"إنشاء {GetDirectionLabel(direction)}: {command.Name}"
        });

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(debtId);
    }

    private static string GetDirectionLabel(DebtDirection direction) => direction switch
    {
        DebtDirection.Receivable => "ذمة مدينة",
        DebtDirection.Payable => "ذمة دائنة",
        _ => "دين"
    };
}