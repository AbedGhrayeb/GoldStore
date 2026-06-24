using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Finance.Accounts.Create;

internal sealed class CreateFinancialAccountCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CreateFinancialAccountCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateFinancialAccountCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Currency>(command.Currency, ignoreCase: true, out var currency))
        {
            return Result.Failure<Guid>(Error.Problem("Finance.InvalidCurrency", "العملة غير صالحة"));
        }

        bool nameExists = await context.FinancialAccounts
            .AsNoTracking()
            .AnyAsync(a => a.Name == command.Name && a.Currency == currency, cancellationToken);

        if (nameExists)
        {
            return Result.Failure<Guid>(Error.Conflict("Finance.DuplicateAccount", "يوجد حساب بنفس الاسم والعملة"));
        }

        var account = new FinancialAccount
        {
            Id = Guid.CreateVersion7(),
            Name = command.Name,
            Currency = currency,
            AccountType = FinancialAccountType.Bank,
            AccountNumber = command.AccountNumber ?? $"{currency}-{Random.Shared.Next(1000, 9999)}",
            Notes = command.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.FinancialAccounts.Add(account);
        await context.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}