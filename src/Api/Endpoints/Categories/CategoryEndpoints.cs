using Application.Categories;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.Categories;

internal class CategoryEndpoints : IEndpoint
{
    //public  RouteGroupBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    //{
    //    var group = app.MapGroup("/api/categories")
    //        .WithTags(Tags.Categories);
    //    group.MapGet("/", async () =>
    //    {
    //        // Implementation for getting categories
    //        return Results.Ok(new { Message = "Get categories endpoint" });
    //    });
    //    return group;
    //}

    public void MapEndpoints(IEndpointRouteBuilder app, ApiVersionSet apiVersionSet)
    {
        var endpoints = app.MapGroup("/api/categories")
            .WithApiVersionSet(apiVersionSet)
            .HasApiVersion(1.0)
            .WithTags(Tags.Categories);

        endpoints.MapGet("/", Get.All)
            .WithName(nameof(Get))
            .WithSummary("Get all categories")
            .WithDescription("Retrieves a list of all categories.")
            .Produces<List<CategoryResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
             .CacheOutput(c => c.Expire(TimeSpan.FromSeconds(60)))
            .MapToApiVersion(1.0);

    }
}
