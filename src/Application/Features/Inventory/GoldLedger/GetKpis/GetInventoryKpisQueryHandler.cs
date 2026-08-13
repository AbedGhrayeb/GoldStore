using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Services;
using Application.Abstractions.Tenants;
using Domain.Common;
using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Inventory.GoldLedger.GetKpis;

internal sealed class GetInventoryKpisQueryHandler(
    IApplicationDbContext context,
    IGoldPriceService goldPriceService,
    ICurrentTenant currentTenant)
    : IQueryHandler<GetInventoryKpisQuery, InventoryKpiResponse>
{
    private static string FormatWeight(decimal grams) => $"{grams:F3}";

    private static string FormatCurrency(decimal amount) => $"{amount:N0}";

    private static string GetKaratDescription(int karat) => karat switch
    {
        24 => "عيار 24",
        21 => "عيار 21",
        18 => "عيار 18",
        _ => $"عيار {karat}"
    };

    public async Task<Result<InventoryKpiResponse>> Handle(GetInventoryKpisQuery query, CancellationToken cancellationToken)
    {
        var karatAggregates = await context.GoldLedgerEntries
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .GroupBy(e => e.Karat)
            .Select(g => new
            {
                Karat = g.Key,
                NetWeight = g.Sum(e => e.MovementType == GoldMovementType.Increase ? e.WeightInGrams : -e.WeightInGrams),
                NetEquivalent21K = g.Sum(e => e.MovementType == GoldMovementType.Increase ? e.Equivalent21KWeightInGrams : -e.Equivalent21KWeightInGrams)
            })
            .ToListAsync(cancellationToken);

        decimal[] supportedKarats = SupportedValues.Karats.Cast<int>().Select(k => (decimal)k).ToArray();

        var breakdowns = supportedKarats
            .OrderByDescending(k => k)
            .Select(k =>
            {
                decimal totalWeight = karatAggregates
                    .Where(a => (int)a.Karat == k)
                    .Select(a => Math.Max(a.NetWeight, 0m))
                    .FirstOrDefault();

                return new KaratBreakdown
                {
                    Karat = (int)k,
                    KaratLabel = $"عيار {(int)k}",
                    Description = GetKaratDescription((int)k),
                    TotalWeightGrams = totalWeight,
                    TotalWeightDisplay = FormatWeight(totalWeight),
                    Unit = "جم",
                    IsPrimary = (int)k == 21
                };
            }).ToList();

        decimal totalEquivalent21K = Math.Max(karatAggregates.Sum(a => a.NetEquivalent21K), 0m);

        GoldPriceData? priceData = null;
        try
        {
            priceData = await goldPriceService.GetCurrentPricesAsync(Currency.JOD, cancellationToken);
        }
        catch
        {
            // If gold price API is unavailable, estimated value shows without live pricing
        }

        decimal estimatedValue = totalEquivalent21K * (priceData?.PricePerGram21K ?? 0m);

        return new InventoryKpiResponse
        {
            TotalEquivalent21KGrams = totalEquivalent21K,
            TotalEquivalent21KDisplay = FormatWeight(totalEquivalent21K),
            TotalEquivalent21KUnit = "جم",
            EstimatedValueJod = estimatedValue,
            EstimatedValueDisplay = priceData is not null ? FormatCurrency(estimatedValue) : "—",
            KaratBreakdowns = breakdowns
        };
    }
}
