// <copyright file="GetTodayEmployeeStatsQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.StoreOperations.GetTodayEmployeeStats;

public sealed record GetTodayEmployeeStatsQuery : IQuery<List<EmployeeDayStatsResponse>>;
