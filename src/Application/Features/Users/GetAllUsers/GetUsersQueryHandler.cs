// <copyright file="GetUsersQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Application.Common.Errors;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Users.GetAllUsers;

internal sealed class GetUsersQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetUsersQuery, List<UserResponse>>
{
    public async Task<Result<List<UserResponse>>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        List<UserResponse>? users = await context.Users.AsNoTracking()
             .Where(u => u.TenantId == currentTenant.TenantId)
             .Select(u => new UserResponse
             {
                 Id = u.Id,
                 FirstName = u.FirstName,
                 LastName = u.LastName,
                 Email = u.Email,
                 PhoneNumber = u.PhoneNumber,
                 WhatsappNumber = u.WhatsappNumber,
             })
             .ToListAsync(cancellationToken);

        if (users is null || !users.Any())
        {
            return ApplicationErrors.EmptyUsersList;
        }

        return users;
    }
}
