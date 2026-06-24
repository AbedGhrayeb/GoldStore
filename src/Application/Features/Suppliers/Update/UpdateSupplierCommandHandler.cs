using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Suppliers.Update;

internal sealed class UpdateSupplierCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateSupplierCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateSupplierCommand command, CancellationToken cancellationToken)
    {
        Supplier? supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);

        if (supplier is null)
        {
            return Result.Failure<bool>(SupplierErrors.NotFound(command.Id));
        }

        bool nameExists = await context.Suppliers
            .AnyAsync(s => s.Name == command.Name && s.Id != command.Id, cancellationToken);

        if (nameExists)
        {
            return Result.Failure<bool>(SupplierErrors.DuplicateName);
        }

        supplier.Name = command.Name;
        supplier.PrimaryPhone = command.PrimaryPhone;
        supplier.SecondaryPhone = command.SecondaryPhone;
        supplier.BankAccountNumber = command.BankAccountNumber;
        supplier.Notes = command.Notes;

        if (command.IsActive != supplier.IsActive)
        {
            if (!command.IsActive)
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

            supplier.IsActive = command.IsActive;
        }

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}