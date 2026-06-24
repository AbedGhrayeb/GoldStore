using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Inventory.Adjustments.GetPaged;

internal sealed class GetInventoryAdjustmentsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetInventoryAdjustmentsQuery, PagedInventoryAdjustmentResponse>
{
    private static string GetTypeLabel(InventoryAdjustmentType type) => type switch
    {
        InventoryAdjustmentType.Increase => "زيادة",
        InventoryAdjustmentType.Decrease => "نقصان",
        InventoryAdjustmentType.Damage => "تالف",
        InventoryAdjustmentType.Loss => "مفقود",
        InventoryAdjustmentType.Correction => "تصحيح يدوي",
        _ => type.ToString()
    };

    private static (string Color, string Bg, string Icon) GetTypeStyling(InventoryAdjustmentType type) => type switch
    {
        InventoryAdjustmentType.Increase => ("text-on-primary-container", "bg-primary-container/20", "arrow_upward"),
        InventoryAdjustmentType.Decrease => ("text-error", "bg-error-container/30", "arrow_downward"),
        InventoryAdjustmentType.Damage => ("text-error", "bg-error-container/30", "broken_image"),
        InventoryAdjustmentType.Loss => ("text-error", "bg-error-container/30", "warning"),
        InventoryAdjustmentType.Correction => ("text-on-surface", "bg-surface-container", "edit"),
        _ => ("text-secondary", "bg-surface-container", "sync_alt")
    };

    private static bool IsIncreaseType(InventoryAdjustmentType type) =>
        type == InventoryAdjustmentType.Increase;

    public async Task<Result<PagedInventoryAdjustmentResponse>> Handle(GetInventoryAdjustmentsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<InventoryAdjustment> adjustments = context.InventoryAdjustments.AsNoTracking();

        if (query.FromDate.HasValue)
        {
            adjustments = adjustments.Where(a => a.Date >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            adjustments = adjustments.Where(a => a.Date <= query.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.AdjustmentType) &&
            Enum.TryParse<InventoryAdjustmentType>(query.AdjustmentType, out var adjType))
        {
            adjustments = adjustments.Where(a => a.Type == adjType);
        }

        int totalCount = await adjustments.CountAsync(cancellationToken);

        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        List<Guid> userIds = await adjustments
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => a.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> userNames = await context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}", cancellationToken);

        List<InventoryAdjustment> pagedData = await adjustments
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        List<InventoryAdjustmentResponse> items = pagedData.Select(a =>
        {
            var (color, bg, icon) = GetTypeStyling(a.Type);
            bool isIncrease = IsIncreaseType(a.Type);
            decimal signedWeight = isIncrease ? a.WeightInGrams : -a.WeightInGrams;

            return new InventoryAdjustmentResponse
            {
                Id = a.Id,
                Date = a.Date,
                Type = a.Type.ToString(),
                TypeLabel = GetTypeLabel(a.Type),
                TypeColor = color,
                TypeBg = bg,
                TypeIcon = icon,
                Karat = (int)a.Karat,
                WeightInGrams = a.WeightInGrams,
                SignedWeight = signedWeight,
                Equivalent21KWeightInGrams = a.Equivalent21KWeightInGrams,
                Reason = a.Reason,
                Notes = a.Notes,
                UserName = userNames.GetValueOrDefault(a.UserId, "—")
            };
        }).ToList();

        return new PagedInventoryAdjustmentResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}