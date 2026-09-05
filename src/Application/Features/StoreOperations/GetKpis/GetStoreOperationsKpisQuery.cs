// <copyright file="GetStoreOperationsKpisQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.StoreOperations.GetKpis;

public sealed record GetStoreOperationsKpisQuery : IQuery<StoreOperationsKpiResponse>;
