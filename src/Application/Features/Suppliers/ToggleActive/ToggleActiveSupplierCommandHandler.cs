using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Suppliers.ToggleActive;

internal sealed class ToggleActiveSupplierCommandHandler(IApplicationDbContext context)
    : ICommandHandler<ToggleActiveSupplierCommand, bool>
{
    public async Task<Result<bool>> Handle(ToggleActiveSupplierCommand command, CancellationToken cancellationToken)
    {
        Supplier? supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);

        if (supplier is null)
        {
            return Result.Failure<bool>(SupplierErrors.NotFound(command.Id));
        }

        if (supplier.IsActive)
        {
            decimal goldBalance = await context.SupplierGoldLedgerEntries
                .Where(e => e.SupplierId == command.Id)
                .SumAsync(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Equivalent21KWeightInGrams : -e.Equivalent21KWeightInGrams, cancellationToken);

            decimal mfgBalance = await context.SupplierManufacturingLedgerEntries
                .Where(e => e.SupplierId == command.Id)
                .SumAsync(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Amount : -e.Amount, cancellationToken);

            if (goldBalance != 0 || mfgBalance != 0)
            {
                return Result.Failure<bool>(SupplierErrors.HasActiveBalance);
            }
        }

        supplier.IsActive = !supplier.IsActive;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}