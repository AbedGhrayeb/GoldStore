// <copyright file="UpdateUserCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Users.Update;

internal sealed class UpdateUserCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IUserContext userContext,
    IRefreshTokenService refreshTokenService)
    : ICommandHandler<UpdateUserCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        if (userContext.UserId != command.Id)
        {
            return UserErrors.Unauthorized();
        }

        User user = await context.Users.FirstOrDefaultAsync(u => u.Id == command.Id, cancellationToken);

        if (user is null)
        {
            return UserErrors.NotFound(command.Id);
        }

        bool passwordChanged = !string.IsNullOrWhiteSpace(command.Password);

        user.Update(command.FirstName, command.LastName, passwordChanged ? passwordHasher.Hash(command.Password!) : null, command.PhoneNumber, command.WhatsappNumber);

        if (passwordChanged)
        {
            // A new password invalidates every previously issued session: rotate the
            // security stamp and revoke all refresh tokens (plan Phase 4 items 3 and 5).
            user.RegenerateSecurityStamp();
            await refreshTokenService.RevokeAllForUserAsync(user.Id, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
