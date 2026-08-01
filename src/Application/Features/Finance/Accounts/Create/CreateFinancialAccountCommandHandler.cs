using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Finance.Accounts.Create;

internal sealed class CreateFinancialAccountCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CreateFinancialAccountCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateFinancialAccountCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Currency>(command.Currency, ignoreCase: true, out Currency currency))
        {
            return Error.Validation("Finance.InvalidCurrency", "العملة غير صالحة");
        }

        bool nameExists = await context.FinancialAccounts
            .AsNoTracking()
            .AnyAsync(a => a.Name == command.Name && a.Currency == currency, cancellationToken);

        if (nameExists)
        {
            return Error.Conflict("Finance.DuplicateAccount", "يوجد حساب بنفس الاسم والعملة");
        }

        Result<FinancialAccount> accountResult = FinancialAccount.Create(command.Name, currency, FinancialAccountType.Bank,
            command.AccountNumber ?? $"{currency}-{Random.Shared.Next(1000, 9999)}", command.Notes);

        if (accountResult.IsError)
        {
            return accountResult.Errors;
        }
        context.FinancialAccounts.Add(accountResult.Value);

        if (command.OpeningBalance >= 0m)
        {
            Result<FinancialTransaction> financialTransactionResult = FinancialTransaction.Create(accountResult.Value.Id, currency, command.OpeningBalance,
                FinancialTransactionType.Inflow, FinancialReferenceType.ManualAdjustment, accountResult.Value.Id,
                string.IsNullOrWhiteSpace(command.Notes) ? "رصيد افتتاحي" : command.Notes);
            if (financialTransactionResult.IsError)
            {
                return financialTransactionResult.Errors;
            }

            context.FinancialTransactions.Add(financialTransactionResult.Value);
        }

        await context.SaveChangesAsync(cancellationToken);

        return accountResult.Value.Id;
    }
}
