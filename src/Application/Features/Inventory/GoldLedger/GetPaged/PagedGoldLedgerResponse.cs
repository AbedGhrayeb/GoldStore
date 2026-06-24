namespace Application.Features.Inventory.GoldLedger.GetPaged;

public sealed record PagedGoldLedgerResponse
{
    public List<GoldLedgerEntryResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}