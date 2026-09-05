// <copyright file="GetInventoryAdjustmentsQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Application.Common.Models;
using Domain.Common;
using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Inventory.Adjustments.GetPaged;

internal sealed class GetInventoryAdjustmentsQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetInventoryAdjustmentsQuery, PaginatedList<InventoryAdjustmentResponse>>
{
    private static bool IsIncreaseType(InventoryAdjustmentType type) =>
        type == InventoryAdjustmentType.Increase;

    public async Task<Result<PaginatedList<InventoryAdjustmentResponse>>> Handle(GetInventoryAdjustmentsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<InventoryAdjustment> adjustments = context.InventoryAdjustments.AsNoTracking().Where(a => a.TenantId == currentTenant.TenantId);

        if (query.FromDate.HasValue)
        {
            DateTime fromDate = DateTime.SpecifyKind(query.FromDate.Value, DateTimeKind.Utc);
            adjustments = adjustments.Where(a => a.CreatedAtUtc >= fromDate);
        }

        if (query.ToDate.HasValue)
        {
            DateTime toDate = DateTime.SpecifyKind(query.ToDate.Value, DateTimeKind.Utc);
            adjustments = adjustments.Where(a => a.CreatedAtUtc <= toDate);
        }

        if (!string.IsNullOrWhiteSpace(query.AdjustmentType) &&
            Enum.TryParse<InventoryAdjustmentType>(query.AdjustmentType, out InventoryAdjustmentType adjType))
        {
            adjustments = adjustments.Where(a => a.Type == adjType);
        }

        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        List<Guid> userIds = await adjustments
            .OrderByDescending(a => a.CreatedAtUtc)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => a.CreatedBy!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> userNames = await context.Users
            .AsNoTracking()
            .Where(u => u.TenantId == currentTenant.TenantId && userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}", cancellationToken);

        IQueryable<InventoryAdjustmentResponse> items = adjustments.Select(a => new InventoryAdjustmentResponse
        {
            Id = a.Id,
            Date = a.CreatedAtUtc!.Value.LocalDateTime,
            Type = a.Type.ToString(),
            TypeLabel = a.Type.GetTypeLabel(),
            TypeColor = a.Type.GetTypeStyling().Color,
            TypeBg = a.Type.GetTypeStyling().Bg,
            TypeIcon = a.Type.GetTypeStyling().Icon,
            Karat = a.Karat.KaratLabel(),
            WeightInGrams = a.WeightInGrams,
            SignedWeight = IsIncreaseType(a.Type) ? a.WeightInGrams : -a.WeightInGrams,
            Equivalent21KWeightInGrams = a.Equivalent21KWeightInGrams,
            Reason = a.Reason,
            Notes = a.Notes,
            UserName = userNames.GetValueOrDefault(a.CreatedBy!.Value, "Unknown"),
        });

        return await PaginatedList<InventoryAdjustmentResponse>.CreateAsync(items, query.Page, query.PageSize);
    }
}
