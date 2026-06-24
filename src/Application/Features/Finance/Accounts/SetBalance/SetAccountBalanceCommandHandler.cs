using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Finance.Accounts.SetBalance;

internal sealed class SetAccountBalanceCommandHandler(IApplicationDbContext context)
    : ICommandHandler<SetAccountBalanceCommand>
{
    public async Task<Result> Handle(SetAccountBalanceCommand command, CancellationToken cancellationToken)
    {
        if (command.TargetBalance < 0m)
        {
            return Result.Failure(FinancialAccountErrors.InvalidTargetBalance);
        }

        FinancialAccount? account = await context.FinancialAccounts
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(FinancialAccountErrors.NotFound(command.AccountId));
        }

        if (!account.IsActive)
        {
            return Result.Failure(FinancialAccountErrors.Inactive);
        }

        decimal currentBalance = await context.FinancialTransactions
            .Where(t => t.AccountId == account.Id)
            .SumAsync(t => t.TransactionType == FinancialTransactionType.Inflow ? t.Amount : -t.Amount, cancellationToken);

        decimal diff = command.TargetBalance - currentBalance;

        if (diff == 0m)
        {
            return Result.Success();
        }

        context.FinancialTransactions.Add(new FinancialTransaction
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Currency = account.Currency,
            Amount = Math.Abs(diff),
            TransactionType = diff > 0m ? FinancialTransactionType.Inflow : FinancialTransactionType.Outflow,
            ReferenceType = FinancialReferenceType.ManualAdjustment,
            ReferenceId = null,
            Date = DateTime.UtcNow,
            Notes = command.Notes
        });

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
