using Application.Abstractions.Messaging;

namespace Application.Features.Inventory.Adjustments.GetPaged;

public sealed record GetInventoryAdjustmentsQuery(
    int Page = 1,
    int PageSize = 20,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? AdjustmentType = null) : IQuery<PagedInventoryAdjustmentResponse>;