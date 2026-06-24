using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Debts;
using Domain.Finance;
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

        var debtId = Guid.NewGuid();

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
            Id = Guid.NewGuid(),
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

        if (command.AccountId.HasValue)
        {
            context.FinancialTransactions.Add(new FinancialTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = command.AccountId.Value,
                Currency = currency,
                Amount = command.Amount,
                TransactionType = financialTransactionType,
                ReferenceType = FinancialReferenceType.DebtCreation,
                ReferenceId = debtId,
                Date = command.Date,
                Notes = $"إنشاء {GetDirectionLabel(direction)}: {command.Name}"
            });
        }

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
