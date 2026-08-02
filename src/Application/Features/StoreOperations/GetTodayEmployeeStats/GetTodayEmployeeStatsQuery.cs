using Application.Abstractions.Messaging;

namespace Application.Features.StoreOperations.GetTodayEmployeeStats;

public sealed record GetTodayEmployeeStatsQuery : IQuery<List<EmployeeDayStatsResponse>>;
