using Api.Extensions;
using Application.Abstractions.Messaging;
using Application.Categories;
using Application.Categories.GetAll;

namespace Api.Endpoints.Categories;

internal static class Get
{
    internal static async Task<IResult> All(IQueryHandler<GetCategoriesQuery, List<CategoryResponse>> queryHandler, CancellationToken ct)
    {
        var result = await queryHandler.Handle(new GetCategoriesQuery(), ct);
        return result.Match(Results.Ok, e => e.ToProblem());
    }
}
