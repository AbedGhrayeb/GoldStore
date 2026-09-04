using Application.Abstractions.Messaging;
using Application.Common.Models;
using Application.Features.Finance.Debts;
using Application.Features.Finance.Debts.Create;
using Application.Features.Finance.Debts.GetKpis;
using Application.Features.Finance.Debts.GetPaged;
using Application.Features.Finance.Debts.Payments;
using Application.Features.Finance.Debts.Update;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using SharedKernel;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Tenant-scoped debt, payment, and debt KPI endpoints.
/// </summary>
public sealed class FinanceDebtsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/finance/debts")
            .WithTags("Finance Debts")
            .RequireAuthorization()
            .RequireAuthorization($"feature:{Features.Finance}")
            .RequireAuthorization("finance.view")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/", GetDebts)
            .WithSummary("Return paged debts.")
            .Produces<PaginatedList<DebtResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet("/kpis", GetKpis)
            .WithSummary("Return debt KPIs.")
            .Produces<DebtKpiResponse>(StatusCodes.Status200OK);

        group.MapPost("/", CreateDebt)
                    .RequireAuthorization("finance.manage")
            .WithSummary("Create a debt and its ledger entries.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateDebt)
                    .RequireAuthorization("finance.manage")
            .WithSummary("Update a debt.")
            .Produces<Updated>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/payments", CreateDebtPayment)
                    .RequireAuthorization("finance.manage")
            .WithSummary("Record a payment against a debt.")
            .Produces<Updated>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> GetDebts(
        int? page,
        int? pageSize,
        string? direction,
        string? search,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<PaginatedList<DebtResponse>> result = await dispatcher
            .DispatchAsync<GetDebtsQuery, PaginatedList<DebtResponse>>(
                new GetDebtsQuery(page ?? 1, pageSize ?? 20, direction, search),
                cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetKpis(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<DebtKpiResponse> result = await dispatcher.DispatchAsync<GetDebtKpisQuery, DebtKpiResponse>(
            new GetDebtKpisQuery(), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> CreateDebt(
        CreateDebtRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<CreateDebtCommand, Guid>(
            new CreateDebtCommand(
                request.Name,
                request.Phone,
                request.Direction,
                request.Currency,
                request.AccountId,
                request.Amount,
                request.Notes,
                request.Date),
            cancellationToken);

        return ApiResults.Created(result);
    }

    private static async Task<IResult> UpdateDebt(
        Guid id,
        UpdateDebtRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Updated> result = await dispatcher.DispatchAsync<UpdateDebtCommand, Updated>(
            new UpdateDebtCommand(
                id,
                request.Name,
                request.Phone,
                request.NewAmount,
                request.NewAccountId,
                request.Notes),
            cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> CreateDebtPayment(
        Guid id,
        CreateDebtPaymentRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Updated> result = await dispatcher.DispatchAsync<CreatePaymentCommand, Updated>(
            new CreatePaymentCommand(id, request.AccountId, request.Amount, request.Date, request.Notes),
            cancellationToken);

        return ApiResults.From(result);
    }
}

public sealed record CreateDebtRequest(
    string Name,
    string? Phone,
    int Direction,
    string Currency,
    Guid AccountId,
    decimal Amount,
    string? Notes,
    DateTime Date);

public sealed record UpdateDebtRequest(
    string? Name,
    string? Phone,
    decimal? NewAmount,
    Guid? NewAccountId,
    string? Notes);

public sealed record CreateDebtPaymentRequest(
    Guid AccountId,
    decimal Amount,
    DateTime Date,
    string? Notes);
