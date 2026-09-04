using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Result;

namespace Application.Features.Inventory.GoldLedger.GetTrend;

internal sealed class GetGoldLedgerTrendQueryHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    ICurrentTenant currentTenant)
    : IQueryHandler<GetGoldLedgerTrendQuery, List<GoldTrendPoint>>
{
    public async Task<Result<List<GoldTrendPoint>>> Handle(GetGoldLedgerTrendQuery query, CancellationToken cancellationToken)
    {
        int days = Math.Clamp(query.Days, 1, 90);
        DateTime todayStart = dateTimeProvider.Now.Date;
        DateTime fromDate = todayStart.AddDays(-(days - 1));

        var entries = await context.GoldLedgerEntries
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId && e.CreatedAtUtc >= fromDate)
            .Select(e => new { e.CreatedAtUtc!.Value.Date, e.MovementType, e.Equivalent21KWeightInGrams })
            .ToListAsync(cancellationToken);

        var aggregates = entries
            .GroupBy(e => e.Date)
            .Select(g => new
            {
                Date = g.Key,
                In21K = g.Where(e => e.MovementType == GoldMovementType.Increase).Sum(e => e.Equivalent21KWeightInGrams),
                Out21K = g.Where(e => e.MovementType == GoldMovementType.Decrease).Sum(e => e.Equivalent21KWeightInGrams)
            })
            .ToDictionary(a => a.Date);

        var points = new List<GoldTrendPoint>(days);
        for (int i = 0; i < days; i++)
        {
            DateTime day = fromDate.AddDays(i);
            aggregates.TryGetValue(day, out var aggregate);
            decimal in21K = aggregate?.In21K ?? 0m;
            decimal out21K = aggregate?.Out21K ?? 0m;

            points.Add(new GoldTrendPoint
            {
                Date = day,
                Label = GetDayLabel(day),
                In21K = in21K,
                Out21K = out21K,
                Net21K = in21K - out21K
            });
        }

        return points;
    }

    private static string GetDayLabel(DateTime day) => day.DayOfWeek switch
    {
        DayOfWeek.Saturday => "السبت",
        DayOfWeek.Sunday => "الأحد",
        DayOfWeek.Monday => "الاثنين",
        DayOfWeek.Tuesday => "الثلاثاء",
        DayOfWeek.Wednesday => "الأربعاء",
        DayOfWeek.Thursday => "الخميس",
        DayOfWeek.Friday => "الجمعة",
        _ => day.ToString("dd/MM")
    };
}
