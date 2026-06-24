namespace Application.Features.Inventory.Adjustments.GetPaged;

public sealed record PagedInventoryAdjustmentResponse
{
    public List<InventoryAdjustmentResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}