using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Suppliers.Update;

internal sealed class UpdateSupplierCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateSupplierCommand, Updated>
{
    public async Task<Result<Updated>> Handle(UpdateSupplierCommand command, CancellationToken cancellationToken)
    {
        Supplier? supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);

        if (supplier is null)
        {
            return SupplierErrors.NotFound(command.Id);
        }

        bool nameExists = await context.Suppliers
            .AnyAsync(s => s.Name == command.Name && s.Id != command.Id, cancellationToken);

        if (nameExists)
        {
            return SupplierErrors.DuplicateName;
        }
        Result<Updated> supplierUpdateResult = supplier.Update(command.Id, command.Name, command.PrimaryPhone, command.SecondaryPhone, command.BankAccountNumber, command.Notes);
        if (supplierUpdateResult.IsError)
        {
            return supplierUpdateResult.Errors;
        }
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
                    return SupplierErrors.HasActiveBalance;
                }
            }

            supplier.IsActive = command.IsActive;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
