// <copyright file="GetPhoneTwoFactorStatusQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Users.TwoFactor;

internal sealed class GetPhoneTwoFactorStatusQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetPhoneTwoFactorStatusQuery, PhoneTwoFactorStatusResponse>
{
    public async Task<Result<PhoneTwoFactorStatusResponse>> Handle(GetPhoneTwoFactorStatusQuery query, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);

        if (user is null)
        {
            return Application.Common.Errors.ApplicationErrors.LoginFailed;
        }

        string? masked = null;
        if (!string.IsNullOrWhiteSpace(user.PhoneNumber) && user.PhoneNumber.Length > 4)
        {
            masked = string.Concat("***", user.PhoneNumber[^4..]);
        }

        int unused = await context.UserRecoveryCodes
            .IgnoreQueryFilters()
            .CountAsync(c => c.UserId == user.Id && !c.IsUsed, cancellationToken);

        return new PhoneTwoFactorStatusResponse(user.TwoFactorEnabled, user.PhoneNumberVerified, masked, new[] { unused.ToString() });
    }
}
