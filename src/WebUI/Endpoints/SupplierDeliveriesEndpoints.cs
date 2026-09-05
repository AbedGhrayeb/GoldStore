// <copyright file="SupplierDeliveriesEndpoints.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.SupplierDeliveries.Create;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Tenant-scoped supplier delivery endpoints. Equivalent 21K weight is always calculated
/// by the domain and is never accepted from the request.
/// </summary>
public sealed class SupplierDeliveriesEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/supplier-deliveries")
            .WithTags("Supplier Deliveries")
            .RequireAuthorization()
            .RequireAuthorization($"feature:{Features.Suppliers}")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/", CreateDelivery)
            .WithSummary("Record a supplier delivery.")
            .WithDescription("Creates one delivery per line and calculates equivalent 21K weights server-side.")
            .Produces<string>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> CreateDelivery(
        CreateSupplierDeliveryRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        if (request.Lines?.Any(line => line is null) is true)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["lines"] = ["Delivery lines cannot contain null items."],
            });
        }

        List<DeliveryLineDto> lines = request.Lines?
            .Select(line => new DeliveryLineDto(line!.Karat, line.WeightInGrams))
            .ToList() ?? [];

        Result<string> result = await dispatcher.DispatchAsync<CreateSupplierDeliveryCommand, string>(
            new CreateSupplierDeliveryCommand(
                request.SupplierId,
                lines,
                request.ManufacturingFeePerGram,
                request.ManufacturingFeeCurrency,
                request.Notes),
            cancellationToken);

        return ApiResults.Created(result);
    }
}

public sealed record CreateSupplierDeliveryRequest(
    Guid SupplierId,
    List<SupplierDeliveryLineRequest?>? Lines,
    decimal ManufacturingFeePerGram,
    string ManufacturingFeeCurrency,
    string? Notes);

public sealed record SupplierDeliveryLineRequest(int Karat, decimal WeightInGrams);
