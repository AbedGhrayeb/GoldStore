using Application.Abstractions.Messaging;
using Application.Features.StoreOperations.GetDetail;
using Application.Features.StoreOperations.GetEmployees;
using Application.Features.StoreOperations.GetKpis;
using Application.Features.StoreOperations.GetPaged;
using Application.Features.StoreOperations.GetTodayEmployeeStats;
using Application.Features.StoreOperations.Shared;
using Microsoft.AspNetCore.Builder;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Tenant-scoped store-operations dashboard endpoints for plan Phase 7a (A6-B5).
/// The dashboard aggregates sales, purchases, and employees across modules, so the
/// group is authentication-only (no feature gate).
/// </summary>
public sealed class StoreOperationsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/dashboard/store-operations")
            .WithTags("Dashboard - Store Operations")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/", GetPaged)
            .WithSummary("Return paged store operations (sales and purchases).")
            .Produces<PagedStoreOperationsResponse>(StatusCodes.Status200OK);

        group.MapGet("/kpis", GetKpis)
            .WithSummary("Return today's store-operations KPIs.")
            .Produces<StoreOperationsKpiResponse>(StatusCodes.Status200OK);

        group.MapGet("/employees", GetEmployees)
            .WithSummary("List employees as dashboard filter options.")
            .Produces<List<EmployeeResponse>>(StatusCodes.Status200OK);

        group.MapGet("/today-employee-stats", GetTodayEmployeeStats)
            .WithSummary("Return today's per-employee sales and purchase stats.")
            .Produces<List<EmployeeDayStatsResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}/detail", GetDetail)
            .WithSummary("Return the detail of one store operation.")
            .Produces<StoreOperationDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetPaged(
        int? page,
        int? pageSize,
        DateTime? fromDate,
        DateTime? toDate,
        string? operationType,
        Guid? employeeId,
        Guid? accountId,
        string? search,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<PagedStoreOperationsResponse> result = await dispatcher
            .DispatchAsync<GetStoreOperationsQuery, PagedStoreOperationsResponse>(
                new GetStoreOperationsQuery(
                    page ?? 1,
                    pageSize ?? 20,
                    fromDate,
                    toDate,
                    operationType,
                    employeeId,
                    accountId,
                    search),
                cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetKpis(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<StoreOperationsKpiResponse> result = await dispatcher
            .DispatchAsync<GetStoreOperationsKpisQuery, StoreOperationsKpiResponse>(
                new GetStoreOperationsKpisQuery(), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetEmployees(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<List<EmployeeResponse>> result = await dispatcher
            .DispatchAsync<GetStoreEmployeesQuery, List<EmployeeResponse>>(
                new GetStoreEmployeesQuery(), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetTodayEmployeeStats(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<List<EmployeeDayStatsResponse>> result = await dispatcher
            .DispatchAsync<GetTodayEmployeeStatsQuery, List<EmployeeDayStatsResponse>>(
                new GetTodayEmployeeStatsQuery(), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetDetail(
        Guid id,
        string operationType,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<StoreOperationDetailResponse> result = await dispatcher
            .DispatchAsync<GetStoreOperationDetailQuery, StoreOperationDetailResponse>(
                new GetStoreOperationDetailQuery(id, operationType), cancellationToken);

        return ApiResults.From(result);
    }
}
