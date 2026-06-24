using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Debts;
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
        {
            return Result.Failure<Guid>(DebtErrors.NotFound(command.Id));
        }

        if (command.Name is not null)
            debt.Name = command.Name;

        if (command.Phone is not null)
            debt.Phone = command.Phone;

        if (command.Notes is not null)
            debt.Notes = command.Notes;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(debt.Id);
    }
}
