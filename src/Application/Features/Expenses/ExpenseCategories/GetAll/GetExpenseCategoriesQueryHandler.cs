using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Expenses.ExpenseCategories.GetAll;

internal sealed class GetExpenseCategoriesQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetExpenseCategoriesQuery, List<ExpenseCategoryResponse>>
{
    public async Task<Result<List<ExpenseCategoryResponse>>> Handle(
        GetExpenseCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        List<ExpenseCategoryResponse> categories = await context.ExpenseCategories
            .AsNoTracking()
            .Where(c => query.ActiveOnly ? c.IsActive : true)
            .OrderByDescending(c => c)
            .Select(c => new ExpenseCategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);

        return categories;
    }
}
