using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Categories.GetAll;

internal sealed class GetCategoriesQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetCategoriesQuery, List<CategoryResponse>>
{
    public async Task<Result<List<CategoryResponse>>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken)
    {
        List<CategoryResponse> categories = await context.Categories
            .Include(x => x.ParentCategory)
            .AsNoTracking()
            .OrderByDescending(c => c)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description ?? "",
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = c.ParentCategoryId.HasValue ? c.ParentCategory.Name : "",
                IsActive = c.IsActive,
            })
            .ToListAsync(cancellationToken);

        return categories;
    }
}
