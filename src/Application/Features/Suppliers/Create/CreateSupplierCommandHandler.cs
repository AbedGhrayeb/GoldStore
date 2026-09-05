// <copyright file="CreateSupplierCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

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
            return SupplierErrors.DuplicateName;
        }

        Result<Supplier> supplierResult = Supplier.Create(command.Name, command.PrimaryPhone, command.SecondaryPhone, command.BankAccountNumber, command.Notes);

        if (supplierResult.IsError)
        {
            return supplierResult.Errors;
        }

        context.Suppliers.Add(supplierResult.Value);
        await context.SaveChangesAsync(cancellationToken);

        return supplierResult.Value.Id;
    }
}
