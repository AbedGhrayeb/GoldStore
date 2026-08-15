using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Application.Features.CustomerPurchaseInvoices.Create;
using Application.Features.CustomerPurchaseInvoices.GetNextNumber;
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
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/", CreateInvoice)
            .WithSummary("Create a customer gold purchase invoice.")
            .WithDescription("Creates the invoice, payment/debt records, and gold-ledger IN entries server-side.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/next-number", GetNextNumber)
            .WithSummary("Return the next tenant-local customer purchase invoice number.")
            .Produces<string>(StatusCodes.Status200OK);
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

        List<PaymentLegDto>? paymentLegs = request.PaymentLegs?
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

    private static async Task<IResult> GetNextNumber(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<string> result = await dispatcher
            .DispatchAsync<GetNextCustomerPurchaseInvoiceNumberQuery, string>(
                new GetNextCustomerPurchaseInvoiceNumberQuery(), cancellationToken);

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
    string SellerIdNumber,
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
