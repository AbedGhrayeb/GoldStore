// <copyright file="GetStoreOperationDetailQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Features.StoreOperations.Shared;

namespace Application.Features.StoreOperations.GetDetail;

public sealed record GetStoreOperationDetailQuery(Guid Id, string OperationType)
    : IQuery<StoreOperationDetailResponse>;
