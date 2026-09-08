// <copyright file="GoldPricesEndpoints.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Features.GoldPrices.GetCurrent;
using Microsoft.AspNetCore.Builder;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Gold price feed endpoints (plan Phase 7a, A6-B1). The price is a live external feed
/// (<see cref="IGoldPriceService"/>), not a tenant-owned entity, so the group only
/// requires authentication — no feature gate.
/// </summary>
public sealed class GoldPricesEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/gold-prices")
            .WithTags("Gold Prices")
            .RequireAuthorization();

        group.MapGet("/current", GetCurrent)
            .WithSummary("Return the current gold price feed.")
            .Produces<GoldPricesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetCurrent(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<GoldPricesResponse> result = await dispatcher.DispatchAsync<GetGoldPricesQuery, GoldPricesResponse>(
            new GetGoldPricesQuery(), cancellationToken);

        return ApiResults.From(result);
    }
}
