// <copyright file="CreateUserCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Subscriptions;
using Application.Abstractions.Tenants;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Users.Create;

internal sealed class CreateUserCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ICurrentTenant currentTenant,
    ISubscriptionGate subscriptionGate)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Central quota gate (plan Phase 4 item 7): active-user plan limit.
        Result<Success> quota = await subscriptionGate.EnsureCanAddUsersAsync(additionalUsers: 1, cancellationToken);
        if (quota.IsError)
        {
            return quota.Errors;
        }

        // Email addresses are unique system-wide, so the uniqueness check must
        // bypass the tenant query filter.
        if (await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == command.Email, cancellationToken))
        {
            return UserErrors.EmailNotUnique;
        }

        Result<User> user = User.Create(Guid.CreateVersion7(), currentTenant.TenantId, command.Email, command.FirstName, command.LastName, passwordHasher.Hash(command.Password), command.PhoneNumber, command.WhatsappNumber);
        if (user.IsError)
        {
            return user.Errors;
        }

        context.Users.Add(user.Value);
        await context.SaveChangesAsync(cancellationToken);

        return user.Value.Id;
    }
}
