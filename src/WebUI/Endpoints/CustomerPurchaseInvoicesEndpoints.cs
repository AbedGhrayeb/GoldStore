// <copyright file="CustomerPurchaseInvoicesEndpoints.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Application.Common.Models;
using Application.Features.CustomerPurchaseInvoices;
using Application.Features.CustomerPurchaseInvoices.Create;
using Application.Features.CustomerPurchaseInvoices.GetById;
using Application.Features.CustomerPurchaseInvoices.GetKpis;
using Application.Features.CustomerPurchaseInvoices.GetPaged;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Tenant-scoped customer gold purchase invoice endpoints.
/// </summary>
public sealed class CustomerPurchaseInvoicesEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/customer-purchases/invoices")
            .WithTags("Customer Purchase Invoices")
            .RequireAuthorization()
            .RequireAuthorization($"feature:{Features.Purchases}")
            .RequireAuthorization("purchases.view")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/", CreateInvoice)
                    .RequireAuthorization("purchases.manage")
            .WithSummary("Create a customer gold purchase invoice.")
            .WithDescription("Creates the invoice, payment/debt records, and gold-ledger IN entries server-side.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", GetInvoices)
            .WithSummary("Return paged customer purchase invoices.")
            .Produces<PaginatedList<CustomerPurchaseInvoiceResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet("/kpis", GetKpis)
            .WithSummary("Return customer purchase invoice KPIs.")
            .Produces<CustomerPurchaseInvoiceKpiResponse>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", GetInvoiceById)
            .WithSummary("Return a customer purchase invoice by ID.")
            .Produces<CustomerPurchaseInvoiceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateInvoice(
        CreateCustomerPurchaseInvoiceRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        IResult? collectionError = ValidateCollections(request.Items, request.PaymentLegs);
        if (collectionError is not null)
        {
            return collectionError;
        }

        List<CustomerPurchaseInvoiceItemDto> items = request.Items?
            .Select(item => new CustomerPurchaseInvoiceItemDto(
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

        Result<Guid> result = await dispatcher.DispatchAsync<CreateCustomerPurchaseInvoiceCommand, Guid>(
            new CreateCustomerPurchaseInvoiceCommand(
                request.SellerName,
                request.SellerPhone,
                request.SellerIdNumber,
                request.SellerYearOfBirth,
                request.SellerAddress,
                request.EmployeeId,
                request.Currency,
                request.Date,
                request.TotalAmount,
                request.AmountPaid,
                request.PaymentMethod,
                request.AccountId,
                request.SellerAccountNumber,
                request.Notes,
                items,
                paymentLegs),
            cancellationToken);

        return ApiResults.Created(result);
    }

    private static async Task<IResult> GetInvoices(
        int? page,
        int? pageSize,
        DateTime? fromDate,
        DateTime? toDate,
        string? search,
        Guid? categoryId,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<PaginatedList<CustomerPurchaseInvoiceResponse>> result = await dispatcher
            .DispatchAsync<GetCustomerPurchaseInvoicesQuery, PaginatedList<CustomerPurchaseInvoiceResponse>>(
                new GetCustomerPurchaseInvoicesQuery(
                    page ?? 1,
                    pageSize ?? 20,
                    fromDate,
                    toDate,
                    search,
                    categoryId),
                cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetKpis(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<CustomerPurchaseInvoiceKpiResponse> result = await dispatcher
            .DispatchAsync<GetCustomerPurchaseInvoicesKpisQuery, CustomerPurchaseInvoiceKpiResponse>(
                new GetCustomerPurchaseInvoicesKpisQuery(), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetInvoiceById(
        Guid id,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<CustomerPurchaseInvoiceResponse> result = await dispatcher
            .DispatchAsync<GetCustomerPurchaseInvoiceByIdQuery, CustomerPurchaseInvoiceResponse>(
                new GetCustomerPurchaseInvoiceByIdQuery(id), cancellationToken);

        return ApiResults.From(result);
    }

    private static IResult? ValidateCollections(
        List<CustomerPurchaseInvoiceItemRequest?>? items,
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

public sealed record CreateCustomerPurchaseInvoiceRequest(
    string SellerName,
    string? SellerPhone,
    string? SellerIdNumber,
    int? SellerYearOfBirth,
    string? SellerAddress,
    Guid EmployeeId,
    string Currency,
    DateTime Date,
    decimal TotalAmount,
    decimal AmountPaid,
    int PaymentMethod,
    Guid AccountId,
    string? SellerAccountNumber,
    string? Notes,
    List<CustomerPurchaseInvoiceItemRequest?>? Items,
    List<InvoicePaymentLegRequest?>? PaymentLegs);

public sealed record CustomerPurchaseInvoiceItemRequest(
    Guid? CategoryId,
    int Karat,
    decimal WeightInGrams,
    decimal PricePerGram);
