using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Categories.GetAll;

internal sealed class GetCategoriesQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetCategoriesQuery, List<CategoryResponse>>
{
    public async Task<Result<List<CategoryResponse>>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken)
    {
        List<CategoryResponse> categories = await context.Categories.AsNoTracking()
            .Where(c => c.TenantId == currentTenant.TenantId)
            .OrderByDescending(c => c)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = c.ParentCategoryId.HasValue ? "" : "",
                IsActive = c.IsActive,
            })
            .ToListAsync(cancellationToken);

        var parentIds = categories
            .Where(c => c.ParentCategoryId.HasValue)
            .Select(c => c.ParentCategoryId!.Value)
            .Distinct()
            .ToList();

        if (parentIds.Count > 0)
        {
            Dictionary<Guid, string> parentNames = await context.Categories
                .Where(c => c.TenantId == currentTenant.TenantId)
                .Where(c => parentIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

            categories = categories.Select(c =>
                c.ParentCategoryId.HasValue && parentNames.TryGetValue(c.ParentCategoryId.Value, out string? name)
                    ? c with { ParentCategoryName = name }
                    : c).ToList();
        }

        return categories;
    }
}
