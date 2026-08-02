using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Models;
using Domain.Common;
using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Inventory.GoldLedger.GetPaged;

internal sealed class GetGoldLedgerQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetGoldLedgerQuery, PaginatedList<GoldLedgerEntryResponse>>
{
    public async Task<Result<PaginatedList<GoldLedgerEntryResponse>>> Handle(GetGoldLedgerQuery query, CancellationToken cancellationToken)
    {
        IQueryable<GoldLedgerEntry> entries = context.GoldLedgerEntries.OrderByDescending(c=>c.CreatedAtUtc).AsNoTracking();

        if (query.Karat.HasValue)
        {
            var karat = (Karat)query.Karat.Value;
            entries = entries.Where(e => e.Karat == karat);
        }

        if (query.FromDate.HasValue)
        {
            DateTime fromDate = query.FromDate.Value.ToUniversalTime();
            entries = entries.Where(e => e.CreatedAtUtc >= fromDate);
        }

        if (query.ToDate.HasValue)
        {
            DateTime toDate = query.ToDate.Value.ToUniversalTime();
            entries = entries.Where(e => e.CreatedAtUtc <= toDate);
        }

        if (!string.IsNullOrWhiteSpace(query.ReferenceType) &&
            Enum.TryParse<GoldReferenceType>(query.ReferenceType, out GoldReferenceType refType))
        {
            entries = entries.Where(e => e.ReferenceType == refType);
        }
        List<Guid> userIds = await entries
            .OrderByDescending(e => e.CreatedAtUtc)
            .Select(e => e.CreatedBy!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> userNames = await context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}", cancellationToken);

        IQueryable<GoldLedgerEntryResponse> items = entries.Select(e => new GoldLedgerEntryResponse
        {
            Id = e.Id,
            Date = e.CreatedAtUtc!.Value.LocalDateTime,
            Karat = e.Karat.KaratLabel(),
            KaratLabel = e.Karat.KaratLabel(),
            WeightInGrams = e.WeightInGrams,
            Equivalent21KWeightInGrams = e.Equivalent21KWeightInGrams,
            MovementType = e.MovementType.ToString(),
            MovementLabel = e.MovementType.GetMovementLabel(),
            MovementColor = e.MovementType.GetMovementColor(),
            MovementIcon = e.MovementType.GetMovementIcon(),
            ReferenceType = e.ReferenceType.ToString(),
            ReferenceLabel = e.ReferenceType.GetReferenceLabel(),
            ReferenceId = e.ReferenceId,
            Notes = e.Notes,
            UserId = e.CreatedBy!.Value,
            UserName = userNames.GetValueOrDefault(e.CreatedBy!.Value, "Unknown")
        });

        return await PaginatedList<GoldLedgerEntryResponse>.CreateAsync(items, query.Page, query.PageSize);
    }
}
