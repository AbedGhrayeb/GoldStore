using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Inventory.GoldLedger.GetPaged;

internal sealed class GetGoldLedgerQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetGoldLedgerQuery, PagedGoldLedgerResponse>
{
    private static string GetKaratLabel(int karat) => karat switch
    {
        24 => "ذهب صافي",
        21 => "مشغولات قياسية",
        18 => "مشغولات حديثة",
        _ => $"عيار {karat}"
    };

    private static string GetMovementLabel(GoldMovementType movement) => movement switch
    {
        GoldMovementType.Increase => "إدخال",
        GoldMovementType.Decrease => "إخراج",
        _ => movement.ToString()
    };

    private static string GetMovementColor(GoldMovementType movement) => movement switch
    {
        GoldMovementType.Increase => "text-tertiary-container",
        GoldMovementType.Decrease => "text-error",
        _ => "text-secondary"
    };

    private static string GetMovementIcon(GoldMovementType movement) => movement switch
    {
        GoldMovementType.Increase => "arrow_downward",
        GoldMovementType.Decrease => "arrow_upward",
        _ => "sync_alt"
    };

    private static string GetReferenceLabel(GoldReferenceType reference) => reference switch
    {
        GoldReferenceType.SupplierDelivery => "توريد مورد",
        GoldReferenceType.CustomerGoldPurchase => "شراء ذهب عميل",
        GoldReferenceType.Sale => "بيع",
        GoldReferenceType.SupplierScrapPayment => "دفع كسر مورد",
        GoldReferenceType.InventoryAdjustment => "تسوية جردية",
        _ => reference.ToString()
    };

    public async Task<Result<PagedGoldLedgerResponse>> Handle(GetGoldLedgerQuery query, CancellationToken cancellationToken)
    {
        IQueryable<GoldLedgerEntry> entries = context.GoldLedgerEntries.AsNoTracking();

        if (query.Karat.HasValue)
        {
            var karat = (Karat)query.Karat.Value;
            entries = entries.Where(e => e.Karat == karat);
        }

        if (query.FromDate.HasValue)
        {
            var fromDate = query.FromDate.Value;
            entries = entries.Where(e => e.Date >= fromDate);
        }

        if (query.ToDate.HasValue)
        {
            var toDate = query.ToDate.Value;
            entries = entries.Where(e => e.Date <= toDate);
        }

        if (!string.IsNullOrWhiteSpace(query.ReferenceType) &&
            Enum.TryParse<GoldReferenceType>(query.ReferenceType, out var refType))
        {
            entries = entries.Where(e => e.ReferenceType == refType);
        }

        int totalCount = await entries.CountAsync(cancellationToken);

        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        //List<Guid> userIds = await entries
        //    .OrderByDescending(e => e.Date)
        //    .ThenByDescending(e => e.Id)
        //    .Skip((page - 1) * pageSize)
        //    .Take(pageSize)
        //    .Select(e => e.UserId)
        //    .Distinct()
        //    .ToListAsync(cancellationToken);

        //Dictionary<Guid, string> userNames = await context.Users
        //    .AsNoTracking()
        //    .Where(u => userIds.Contains(u.Id))
        //    .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}", cancellationToken);

        List<GoldLedgerEntry> pagedData = await entries
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        List<GoldLedgerEntryResponse> items = pagedData.Select(e => new GoldLedgerEntryResponse
        {
            Id = e.Id,
            Date = e.Date,
            Karat = (int)e.Karat,
            KaratLabel = GetKaratLabel((int)e.Karat),
            WeightInGrams = e.WeightInGrams,
            Equivalent21KWeightInGrams = e.Equivalent21KWeightInGrams,
            MovementType = e.MovementType.ToString(),
            MovementLabel = GetMovementLabel(e.MovementType),
            MovementColor = GetMovementColor(e.MovementType),
            MovementIcon = GetMovementIcon(e.MovementType),
            ReferenceType = e.ReferenceType.ToString(),
            ReferenceLabel = GetReferenceLabel(e.ReferenceType),
            ReferenceId = e.ReferenceId,
            Notes = e.Notes,
            UserId = e.UserId,
            //UserName = userNames.GetValueOrDefault(e.UserId, "—")
        }).ToList();

        return new PagedGoldLedgerResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
