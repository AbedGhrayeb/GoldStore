using Application.Abstractions.Messaging;
using Application.Common.Models;
using Application.Features.Inventory.Adjustments;
using Application.Features.Inventory.Adjustments.Create;
using Application.Features.Inventory.Adjustments.GetKpis;
using Application.Features.Inventory.Adjustments.GetPaged;
using Application.Features.Inventory.GoldLedger;
using Application.Features.Inventory.GoldLedger.GetKpis;
using Application.Features.Inventory.GoldLedger.GetPaged;
using Application.Features.Inventory.GoldLedger.GetTrend;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Tenant-scoped inventory adjustment, gold-ledger, trend, and KPI endpoints.
/// </summary>
public sealed class InventoryEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/inventory")
            .WithTags("Inventory")
            .RequireAuthorization()
            .RequireAuthorization($"feature:{Features.Inventory}")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/adjustments", CreateAdjustment)
            .WithSummary("Create an inventory adjustment.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/adjustments", GetAdjustments)
            .WithSummary("Return paged inventory adjustments.")
            .Produces<PaginatedList<InventoryAdjustmentResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet("/adjustments/kpis", GetAdjustmentKpis)
            .WithSummary("Return inventory adjustment KPIs.")
            .Produces<InventoryAdjustmentKpiResponse>(StatusCodes.Status200OK);

        group.MapGet("/gold-ledger", GetGoldLedger)
            .WithSummary("Return the paged gold ledger.")
            .Produces<PaginatedList<GoldLedgerEntryResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet("/gold-ledger/trend", GetGoldLedgerTrend)
            .WithSummary("Return recent gold-ledger trend points.")
            .Produces<List<GoldTrendPoint>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet("/gold-ledger/kpis", GetInventoryKpis)
            .WithSummary("Return inventory KPIs by karat.")
            .Produces<InventoryKpiResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> CreateAdjustment(
        CreateInventoryAdjustmentRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<CreateInventoryAdjustmentCommand, Guid>(
            new CreateInventoryAdjustmentCommand(
                request.AdjustmentType,
                request.Karat,
                request.WeightInGrams,
                request.Reason,
                request.Notes,
                request.Date),
            cancellationToken);

        return ApiResults.Created($"/{ApiRoutes.Tenant}/inventory/adjustments", result);
    }

    private static async Task<IResult> GetAdjustments(
        int? page,
        int? pageSize,
        DateTime? fromDate,
        DateTime? toDate,
        string? adjustmentType,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<PaginatedList<InventoryAdjustmentResponse>> result = await dispatcher
            .DispatchAsync<GetInventoryAdjustmentsQuery, PaginatedList<InventoryAdjustmentResponse>>(
                new GetInventoryAdjustmentsQuery(
                    page ?? 1,
                    pageSize ?? 20,
                    fromDate,
                    toDate,
                    adjustmentType),
                cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetAdjustmentKpis(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<InventoryAdjustmentKpiResponse> result = await dispatcher
            .DispatchAsync<GetInventoryAdjustmentKpisQuery, InventoryAdjustmentKpiResponse>(
                new GetInventoryAdjustmentKpisQuery(), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetGoldLedger(
        int? page,
        int? pageSize,
        int? karat,
        DateTime? fromDate,
        DateTime? toDate,
        string? referenceType,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<PaginatedList<GoldLedgerEntryResponse>> result = await dispatcher
            .DispatchAsync<GetGoldLedgerQuery, PaginatedList<GoldLedgerEntryResponse>>(
                new GetGoldLedgerQuery(
                    page ?? 1,
                    pageSize ?? 20,
                    karat,
                    fromDate,
                    toDate,
                    referenceType),
                cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetGoldLedgerTrend(
        int? days,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<List<GoldTrendPoint>> result = await dispatcher.DispatchAsync<GetGoldLedgerTrendQuery, List<GoldTrendPoint>>(
            new GetGoldLedgerTrendQuery(days ?? 7), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetInventoryKpis(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<InventoryKpiResponse> result = await dispatcher.DispatchAsync<GetInventoryKpisQuery, InventoryKpiResponse>(
            new GetInventoryKpisQuery(), cancellationToken);

        return ApiResults.From(result);
    }
}

public sealed record CreateInventoryAdjustmentRequest(
    int AdjustmentType,
    int Karat,
    decimal WeightInGrams,
    string Reason,
    string? Notes,
    DateTime Date);
