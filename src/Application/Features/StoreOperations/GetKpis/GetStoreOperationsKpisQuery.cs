using Application.Abstractions.Messaging;

namespace Application.Features.StoreOperations.GetKpis;

public sealed record GetStoreOperationsKpisQuery : IQuery<StoreOperationsKpiResponse>;
