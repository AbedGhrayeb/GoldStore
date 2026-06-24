using Application.Abstractions.Messaging;

namespace Application.Features.StoreOperations.GetPaged;

public sealed record GetStoreOperationsQuery(
    int Page = 1,
    int PageSize = 20,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? OperationType = null,
    string? EmployeeName = null,
    Guid? AccountId = null,
    string? Search = null) : IQuery<PagedStoreOperationsResponse>;
