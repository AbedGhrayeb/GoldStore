// <copyright file="SuppliersEndpoints.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Common.Models;
using Application.Suppliers;
using Application.Suppliers.Create;
using Application.Suppliers.GetAll;
using Application.Suppliers.GetBalances;
using Application.Suppliers.GetById;
using Application.Suppliers.GetPaged;
using Application.Suppliers.GetTransactions;
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

        group.MapGet("/paged", GetPagedSuppliers)
            .WithSummary("Return paged suppliers with search and status filter.")
            .Produces<PaginatedList<SupplierResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", GetSupplierById)
            .WithSummary("Return a supplier and its balances.")
            .Produces<SupplierDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/balances", GetSupplierBalances)
            .WithSummary("Return a supplier's due gold per karat and due manufacturing per currency.")
            .Produces<SupplierBalancesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/transactions", GetSupplierTransactions)
            .WithSummary("Return a supplier's transactions (gold, manufacturing, financial) with type filter and paging.")
            .Produces<PaginatedList<SupplierTransactionResponse>>(StatusCodes.Status200OK)
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

    private static async Task<IResult> GetPagedSuppliers(
        int? page,
        int? pageSize,
        string? search,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<PaginatedList<SupplierResponse>> result = await dispatcher
            .DispatchAsync<GetPagedSuppliersQuery, PaginatedList<SupplierResponse>>(
                new GetPagedSuppliersQuery(
                    page ?? 1,
                    pageSize ?? 15,
                    search,
                    activeOnly),
                cancellationToken);

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

    private static async Task<IResult> GetSupplierBalances(
        Guid id,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<SupplierBalancesResponse> result = await dispatcher.DispatchAsync<GetSupplierBalancesQuery, SupplierBalancesResponse>(
            new GetSupplierBalancesQuery(id), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetSupplierTransactions(
        Guid id,
        string? type,
        int? page,
        int? pageSize,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<PaginatedList<SupplierTransactionResponse>> result = await dispatcher
            .DispatchAsync<GetSupplierTransactionsQuery, PaginatedList<SupplierTransactionResponse>>(
                new GetSupplierTransactionsQuery(id, type, page ?? 1, pageSize ?? 15),
                cancellationToken);

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
