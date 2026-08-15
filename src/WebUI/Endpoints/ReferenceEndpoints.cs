using Domain.Common;
using Microsoft.AspNetCore.Builder;

namespace WebUI.Endpoints;

/// <summary>
/// Shared reference data endpoints (plan Phase 7a, A6-B1). Karats and currencies are
/// hardcoded domain lists (<see cref="SupportedValues"/>), not database tables, so these
/// routes return them directly without a dispatcher. Auth only — no feature gate.
/// </summary>
public sealed class ReferenceEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/reference")
            .WithTags("Reference Data")
            .RequireAuthorization();

        group.MapGet("/karats", GetKarats)
            .WithSummary("Return the supported gold karat values.")
            .Produces<KaratReference[]>(StatusCodes.Status200OK);

        group.MapGet("/currencies", GetCurrencies)
            .WithSummary("Return the supported currencies.")
            .Produces<CurrencyReference[]>(StatusCodes.Status200OK);
    }

    private static IResult GetKarats()
    {
        KaratReference[] karats = SupportedValues.Karats
            .Select(karat => new KaratReference((int)karat, karat.KaratLabel()))
            .ToArray();

        return TypedResults.Ok(karats);
    }

    private static IResult GetCurrencies()
    {
        CurrencyReference[] currencies = SupportedValues.Currencies
            .Select(currency => new CurrencyReference(
                (int)currency,
                currency.ToCurrencyString(),
                CurrencyExtensions.CurrencyLabels[currency].Symbol))
            .ToArray();

        return TypedResults.Ok(currencies);
    }
}

public sealed record KaratReference(int Value, string Label);

public sealed record CurrencyReference(int Value, string Code, string Symbol);
