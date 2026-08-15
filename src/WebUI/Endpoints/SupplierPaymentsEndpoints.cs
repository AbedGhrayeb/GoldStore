using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Application.SupplierPayments.Manufacturing.Create;
using Application.SupplierPayments.ScrapGold.Create;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Tenant-scoped supplier gold and manufacturing payment endpoints.
/// </summary>
public sealed class SupplierPaymentsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/supplier-payments")
            .WithTags("Supplier Payments")
            .RequireAuthorization()
            .RequireAuthorization($"feature:{Features.Suppliers}")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/scrap-gold", CreateScrapGoldPayment)
            .WithSummary("Record a scrap-gold payment to a supplier.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/manufacturing", CreateManufacturingPayment)
            .WithSummary("Record a manufacturing-fee payment to a supplier.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> CreateScrapGoldPayment(
        CreateSupplierScrapGoldPaymentRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<CreateSupplierScrapGoldPaymentCommand, Guid>(
            new CreateSupplierScrapGoldPaymentCommand(
                request.SupplierId,
                request.Karat,
                request.WeightInGrams,
                request.Notes),
            cancellationToken);

        return ApiResults.Created(result);
    }

    private static async Task<IResult> CreateManufacturingPayment(
        CreateSupplierManufacturingPaymentRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        if (request.PaymentLegs?.Any(leg => leg is null) is true)
        {
            return InvalidPaymentLeg();
        }

        Result<Guid> result = await dispatcher.DispatchAsync<CreateSupplierManufacturingPaymentCommand, Guid>(
            new CreateSupplierManufacturingPaymentCommand(
                request.SupplierId,
                request.AccountId,
                request.Amount,
                request.Currency,
                request.Notes,
                MapPaymentLegs(request.PaymentLegs)),
            cancellationToken);

        return ApiResults.Created(result);
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

public sealed record CreateSupplierScrapGoldPaymentRequest(
    Guid SupplierId,
    int Karat,
    decimal WeightInGrams,
    string? Notes);

public sealed record CreateSupplierManufacturingPaymentRequest(
    Guid SupplierId,
    Guid AccountId,
    decimal Amount,
    string Currency,
    string? Notes,
    List<SupplierPaymentLegRequest?>? PaymentLegs);

public sealed record SupplierPaymentLegRequest(
    Guid AccountId,
    string Currency,
    decimal Amount,
    decimal ExchangeRate);
