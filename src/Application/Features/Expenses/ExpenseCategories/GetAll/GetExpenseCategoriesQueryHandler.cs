// <copyright file="GetExpenseCategoriesQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Expenses.ExpenseCategories.GetAll;

internal sealed class GetExpenseCategoriesQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetExpenseCategoriesQuery, List<ExpenseCategoryResponse>>
{
    public async Task<Result<List<ExpenseCategoryResponse>>> Handle(
        GetExpenseCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        List<ExpenseCategoryResponse> categories = await context.ExpenseCategories
            .AsNoTracking()
            .Where(c => c.TenantId == currentTenant.TenantId)
            .Where(c => query.ActiveOnly ? c.IsActive : true)
            .OrderByDescending(c => c)
            .Select(c => new ExpenseCategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive,
            })
            .ToListAsync(cancellationToken);

        return categories;
    }
}
