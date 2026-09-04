using Application.Abstractions.Messaging;
using Application.Suppliers;
using Application.Suppliers.Create;
using Application.Suppliers.GetAll;
using Application.Suppliers.GetById;
using Application.Suppliers.ToggleActive;
using Application.Suppliers.Update;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using SharedKernel;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Tenant-scoped supplier management endpoints for Phase 7a (A6-B2).
/// </summary>
public sealed class SuppliersEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/suppliers")
            .WithTags("Suppliers")
            .RequireAuthorization()
            .RequireAuthorization($"feature:{Features.Suppliers}")
            .RequireAuthorization("suppliers.view")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/", GetSuppliers)
            .WithSummary("List the store's suppliers.")
            .Produces<List<SupplierResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", GetSupplierById)
            .WithSummary("Return a supplier and its balances.")
            .Produces<SupplierDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateSupplier)
                    .RequireAuthorization("suppliers.manage")
            .WithSummary("Create a supplier.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateSupplier)
                    .RequireAuthorization("suppliers.manage")
            .WithSummary("Update a supplier.")
            .Produces<Updated>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/toggle-active", ToggleActiveSupplier)
                    .RequireAuthorization("suppliers.manage")
            .WithSummary("Toggle a supplier's active state.")
            .Produces<Updated>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetSuppliers(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<List<SupplierResponse>> result = await dispatcher.DispatchAsync<GetSuppliersQuery, List<SupplierResponse>>(
            new GetSuppliersQuery(), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetSupplierById(
        Guid id,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<SupplierDetailResponse> result = await dispatcher.DispatchAsync<GetSupplierByIdQuery, SupplierDetailResponse>(
            new GetSupplierByIdQuery(id), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> CreateSupplier(
        CreateSupplierRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<CreateSupplierCommand, Guid>(
            new CreateSupplierCommand(
                request.Name,
                request.PrimaryPhone,
                request.SecondaryPhone,
                request.BankAccountNumber,
                request.Notes),
            cancellationToken);

        return ApiResults.Created($"/{ApiRoutes.Tenant}/suppliers/{result.Value}", result);
    }

    private static async Task<IResult> UpdateSupplier(
        Guid id,
        UpdateSupplierRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Updated> result = await dispatcher.DispatchAsync<UpdateSupplierCommand, Updated>(
            new UpdateSupplierCommand(
                id,
                request.Name,
                request.PrimaryPhone,
                request.SecondaryPhone,
                request.BankAccountNumber,
                request.Notes,
                request.IsActive),
            cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> ToggleActiveSupplier(
        Guid id,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Updated> result = await dispatcher.DispatchAsync<ToggleActiveSupplierCommand, Updated>(
            new ToggleActiveSupplierCommand(id), cancellationToken);

        return ApiResults.From(result);
    }
}

public sealed record CreateSupplierRequest(
    string Name,
    string PrimaryPhone,
    string? SecondaryPhone,
    string? BankAccountNumber,
    string? Notes);

public sealed record UpdateSupplierRequest(
    string Name,
    string PrimaryPhone,
    string? SecondaryPhone,
    string? BankAccountNumber,
    string? Notes,
    bool IsActive);
