using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Application.Features.SupplierFinancialTransactions.Create;
using Application.Features.SupplierFinancialTransactions.GetKpis;
using Application.Features.SupplierFinancialTransactions.GetPaged;
using Application.Features.SupplierFinancialTransactions.GetPayments;
using Application.Features.SupplierFinancialTransactions.Payments;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Tenant-scoped supplier financial transaction and payment endpoints.
/// </summary>
public sealed class SupplierFinancialTransactionsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/supplier-financial-transactions")
            .WithTags("Supplier Financial Transactions")
            .RequireAuthorization()
            .RequireAuthorization($"feature:{Features.Suppliers}")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/", CreateTransaction)
            .WithSummary("Create a supplier financial transaction.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", GetTransactions)
            .WithSummary("Return paged supplier financial transactions.")
            .Produces<PagedSupplierFinancialTransactionResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet("/{transactionId:guid}/payments", GetPayments)
            .WithSummary("List payments for a supplier financial transaction.")
            .Produces<List<SupplierFinancialPaymentResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{transactionId:guid}/payments", CreatePayment)
            .WithSummary("Record a payment against a supplier financial transaction.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/kpis", GetKpis)
            .WithSummary("Return supplier financial transaction KPIs.")
            .Produces<SupplierFinancialKpiResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> CreateTransaction(
        CreateSupplierFinancialTransactionRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        if (request.PaymentLegs?.Any(leg => leg is null) is true)
        {
            return InvalidPaymentLeg();
        }

        Result<Guid> result = await dispatcher.DispatchAsync<CreateSupplierFinancialTransactionCommand, Guid>(
            new CreateSupplierFinancialTransactionCommand(
                request.SupplierId,
                request.Direction,
                request.Amount,
                request.Currency,
                request.AccountId,
                request.Date,
                request.Notes,
                MapPaymentLegs(request.PaymentLegs)),
            cancellationToken);

        return ApiResults.Created($"/{ApiRoutes.Tenant}/supplier-financial-transactions", result);
    }

    private static async Task<IResult> GetTransactions(
        int? page,
        int? pageSize,
        Guid? supplierId,
        int? direction,
        string? search,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<PagedSupplierFinancialTransactionResponse> result = await dispatcher
            .DispatchAsync<GetPagedSupplierFinancialTransactionsQuery, PagedSupplierFinancialTransactionResponse>(
                new GetPagedSupplierFinancialTransactionsQuery(
                    page ?? 1,
                    pageSize ?? 20,
                    supplierId,
                    direction,
                    search),
                cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetPayments(
        Guid transactionId,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<List<SupplierFinancialPaymentResponse>> result = await dispatcher
            .DispatchAsync<GetPaymentsByTransactionIdQuery, List<SupplierFinancialPaymentResponse>>(
                new GetPaymentsByTransactionIdQuery(transactionId), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> CreatePayment(
        Guid transactionId,
        CreateSupplierFinancialPaymentRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        if (request.PaymentLegs?.Any(leg => leg is null) is true)
        {
            return InvalidPaymentLeg();
        }

        Result<Guid> result = await dispatcher.DispatchAsync<CreateSupplierFinancialPaymentCommand, Guid>(
            new CreateSupplierFinancialPaymentCommand(
                transactionId,
                request.AccountId,
                request.Amount,
                request.Date,
                request.Notes,
                MapPaymentLegs(request.PaymentLegs)),
            cancellationToken);

        return ApiResults.Created(
            $"/{ApiRoutes.Tenant}/supplier-financial-transactions/{transactionId}/payments",
            result);
    }

    private static async Task<IResult> GetKpis(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<SupplierFinancialKpiResponse> result = await dispatcher
            .DispatchAsync<GetSupplierFinancialKpisQuery, SupplierFinancialKpiResponse>(
                new GetSupplierFinancialKpisQuery(), cancellationToken);

        return ApiResults.From(result);
    }

    private static List<PaymentLegDto>? MapPaymentLegs(List<SupplierPaymentLegRequest?>? paymentLegs) =>
        paymentLegs?.Select(leg => new PaymentLegDto(
            leg!.AccountId,
            leg.Currency,
            leg.Amount,
            leg.ExchangeRate)).ToList();

    private static IResult InvalidPaymentLeg() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["paymentLegs"] = ["Payment legs cannot contain null items."],
        });
}

public sealed record CreateSupplierFinancialTransactionRequest(
    Guid SupplierId,
    int Direction,
    decimal Amount,
    string Currency,
    Guid AccountId,
    DateTime Date,
    string? Notes,
    List<SupplierPaymentLegRequest?>? PaymentLegs);

public sealed record CreateSupplierFinancialPaymentRequest(
    Guid AccountId,
    decimal Amount,
    DateTime Date,
    string? Notes,
    List<SupplierPaymentLegRequest?>? PaymentLegs);
