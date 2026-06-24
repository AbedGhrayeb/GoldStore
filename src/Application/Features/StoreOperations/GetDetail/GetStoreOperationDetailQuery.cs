using Application.Abstractions.Messaging;
using Application.Features.StoreOperations.Shared;

namespace Application.Features.StoreOperations.GetDetail;

public sealed record GetStoreOperationDetailQuery(Guid Id, string OperationType)
    : IQuery<StoreOperationDetailResponse>;
