using Application.Features.StoreOperations.Shared;

namespace Application.Features.StoreOperations.GetPaged;

public sealed record PagedStoreOperationsResponse
{
    public List<StoreOperationResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
