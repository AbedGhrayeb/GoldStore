using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Application.Common.Models;
using Application.Features.SalesInvoices;
using Application.Features.SalesInvoices.Create;
using Application.Features.SalesInvoices.GetById;
using Application.Features.SalesInvoices.GetKpis;
using Application.Features.SalesInvoices.GetNextNumber;
using Application.Features.SalesInvoices.GetPaged;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Tenant-scoped sales invoice endpoints.
/// </summary>
public sealed class SalesInvoicesEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/sales-invoices")
            .WithTags("Sales Invoices")
            .RequireAuthorization()
            .RequireAuthorization($"feature:{Features.Sales}")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/", CreateInvoice)
            .RequireAuthorization("sales.manage")
            .WithSummary("Create a sales invoice.")
            .WithDescription("Creates the invoice, payment/debt records, and gold-ledger OUT entries server-side.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", GetInvoices)
            .RequireAuthorization("sales.view")
            .WithSummary("Return paged sales invoices.")
            .Produces<PaginatedList<SalesInvoiceResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", GetInvoiceById)
            .WithSummary("Return a sales invoice by ID.")
            .Produces<SalesInvoiceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/next-number", GetNextNumber)
            .WithSummary("Return the next tenant-local sales invoice number.")
            .Produces<string>(StatusCodes.Status200OK);

        group.MapGet("/kpis", GetKpis)
            .WithSummary("Return sales invoice KPIs.")
            .Produces<SalesInvoiceKpiResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> CreateInvoice(
        CreateSalesInvoiceRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        IResult? collectionError = ValidateCollections(request.Items, request.PaymentLegs);
        if (collectionError is not null)
        {
            return collectionError;
        }

        List<SalesInvoiceItemDto> items = request.Items?
            .Select(item => new SalesInvoiceItemDto(
                item!.CategoryId,
                item.Karat,
                item.WeightInGrams,
                item.PricePerGram))
            .ToList() ?? [];

        var paymentLegs = request.PaymentLegs?
            .Select(leg => new PaymentLegDto(
                leg!.AccountId,
                leg.Currency,
                leg.Amount,
                leg.ExchangeRate))
            .ToList();

        Result<Guid> result = await dispatcher.DispatchAsync<CreateSalesInvoiceCommand, Guid>(
            new CreateSalesInvoiceCommand(
                request.CustomerName,
                request.CustomerPhone,
                request.Date,
                request.Currency,
                items,
                request.TotalAmount,
                request.AmountPaid,
                request.PaymentMethod,
                request.AccountId,
                request.BuyerAccountNumber,
                request.EmployeeId,
                paymentLegs,
                request.Notes),
            cancellationToken);

        return ApiResults.Created(result);
    }

    private static async Task<IResult> GetInvoices(
        int? page,
        int? pageSize,
        DateTime? fromDate,
        DateTime? toDate,
        string? search,
        string? status,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<PaginatedList<SalesInvoiceResponse>> result = await dispatcher
            .DispatchAsync<GetSalesInvoicesQuery, PaginatedList<SalesInvoiceResponse>>(
                new GetSalesInvoicesQuery(
                    page ?? 1,
                    pageSize ?? 20,
                    fromDate,
                    toDate,
                    search,
                    status),
                cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetInvoiceById(
        Guid id,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<SalesInvoiceResponse> result = await dispatcher
            .DispatchAsync<GetSalesInvoiceByIdQuery, SalesInvoiceResponse>(
                new GetSalesInvoiceByIdQuery(id), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetNextNumber(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<string> result = await dispatcher.DispatchAsync<GetNextInvoiceNumberQuery, string>(
            new GetNextInvoiceNumberQuery(), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetKpis(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<SalesInvoiceKpiResponse> result = await dispatcher
            .DispatchAsync<GetSalesInvoiceKpisQuery, SalesInvoiceKpiResponse>(
                new GetSalesInvoiceKpisQuery(), cancellationToken);

        return ApiResults.From(result);
    }

    private static IResult? ValidateCollections(
        List<SalesInvoiceItemRequest?>? items,
        List<InvoicePaymentLegRequest?>? paymentLegs)
    {
        if (items?.Any(item => item is null) is true)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = ["Invoice items cannot contain null entries."],
            });
        }

        if (paymentLegs?.Any(leg => leg is null) is true)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["paymentLegs"] = ["Payment legs cannot contain null entries."],
            });
        }

        return null;
    }
}

public sealed record CreateSalesInvoiceRequest(
    string CustomerName,
    string? CustomerPhone,
    DateTime Date,
    string Currency,
    List<SalesInvoiceItemRequest?>? Items,
    decimal TotalAmount,
    decimal AmountPaid,
    int? PaymentMethod,
    Guid? AccountId,
    string? BuyerAccountNumber,
    Guid? EmployeeId,
    List<InvoicePaymentLegRequest?>? PaymentLegs,
    string? Notes);

public sealed record SalesInvoiceItemRequest(
    Guid? CategoryId,
    int Karat,
    decimal WeightInGrams,
    decimal PricePerGram);

public sealed record InvoicePaymentLegRequest(
    Guid AccountId,
    string Currency,
    decimal Amount,
    decimal ExchangeRate);
