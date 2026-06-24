using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Suppliers.Create;

internal sealed class CreateSupplierCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CreateSupplierCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSupplierCommand command, CancellationToken cancellationToken)
    {
        bool nameExists = await context.Suppliers
            .AnyAsync(s => s.Name == command.Name, cancellationToken);

        if (nameExists)
        {
            return Result.Failure<Guid>(SupplierErrors.DuplicateName);
        }

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            PrimaryPhone = command.PrimaryPhone,
            SecondaryPhone = command.SecondaryPhone,
            BankAccountNumber = command.BankAccountNumber,
            Notes = command.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync(cancellationToken);

        return supplier.Id;
    }
}