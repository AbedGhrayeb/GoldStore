// <copyright file="CategoriesEndpoints.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Categories;
using Application.Categories.Create;
using Application.Categories.Delete;
using Application.Categories.GetAll;
using Application.Categories.ToggleActive;
using Application.Categories.Update;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Category endpoints (plan Phase 7a, A6-B1) mirroring the MVC <c>CategoriesController</c>.
/// All reads and writes are scoped to the bearer token's tenant; category names are
/// unique per tenant (and per parent), enforced by the command handlers and the
/// <c>(TenantId, ParentCategoryId, Name)</c> index.
/// </summary>
public sealed class CategoriesEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/categories")
            .WithTags("Categories")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization($"feature:{Features.Catalog}")
            .RequireAuthorization("inventory.view");

        group.MapGet("/", GetCategories)
            .WithName(nameof(GetCategories))
            .WithSummary("List the store's categories.")
            .WithDescription("Returns a list of all categories for the current tenant, including their parent-child relationships.")
            .Produces<List<CategoryResponse>>(StatusCodes.Status200OK);

        group.MapPost("/", CreateCategory)
            .RequireAuthorization("inventory.manage")
            .WithName(nameof(CreateCategory))
            .WithSummary("Create a new category.")
            .WithDescription("Creates a new category for the current tenant. The category name must be unique within its parent category.")
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateCategory)
            .RequireAuthorization("inventory.manage")
            .WithName(nameof(UpdateCategory))
            .WithSummary("Update a category.")
            .WithDescription("Updates an existing category for the current tenant. The category name must be unique within its parent category.")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/toggle-active", ToggleActiveCategory)
                    .RequireAuthorization("inventory.manage")
                    .WithName(nameof(ToggleActiveCategory))
                    .WithSummary("Toggle a category's active state.")
                    .WithDescription("Toggles the active state of an existing category for the current tenant.")
                    .Produces(StatusCodes.Status200OK)
                    .ProducesProblem(StatusCodes.Status404NotFound)
                    .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteCategory)
            .RequireAuthorization("inventory.manage")
            .WithName(nameof(DeleteCategory))
            .WithSummary("Delete a category.")
            .WithDescription("Deletes a category for the current tenant. Fails when the category has children or is referenced by sales or purchase invoice items.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> GetCategories(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<List<CategoryResponse>> result = await dispatcher.DispatchAsync<GetCategoriesQuery, List<CategoryResponse>>(
            new GetCategoriesQuery(), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> CreateCategory(
        CreateCategoryRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<CreateCategoryCommand, Guid>(
            new CreateCategoryCommand(request.Name, request.Description, request.ParentCategoryId, request.IsActive, request.WeightInGrams, request.Karat),
            cancellationToken);

        return ApiResults.Created($"/{ApiRoutes.Tenant}/categories/{result.Value}", result);
    }

    private static async Task<IResult> UpdateCategory(
        Guid id,
        UpdateCategoryRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Updated> result = await dispatcher.DispatchAsync<UpdateCategoryCommand, Updated>(
            new UpdateCategoryCommand(id, request.Name, request.Description, request.ParentCategoryId, request.IsActive, request.WeightInGrams, request.Karat),
            cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> ToggleActiveCategory(
            Guid id,
            ICommandDispatcher dispatcher,
            CancellationToken cancellationToken)
    {
        Result<Updated> result = await dispatcher.DispatchAsync<ToggleActiveCategoryCommand, Updated>(
            new ToggleActiveCategoryCommand(id), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> DeleteCategory(
        Guid id,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Deleted> result = await dispatcher.DispatchAsync<DeleteCategoryCommand, Deleted>(
            new DeleteCategoryCommand(id), cancellationToken);

        return ApiResults.From(result);
    }
}

public sealed record CreateCategoryRequest(string Name, string? Description, Guid? ParentCategoryId, bool IsActive, decimal WeightInGrams, int Karat);

public sealed record UpdateCategoryRequest(string Name, string? Description, Guid? ParentCategoryId, bool IsActive, decimal WeightInGrams, int Karat);
