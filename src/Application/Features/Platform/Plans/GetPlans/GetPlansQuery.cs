using Application.Abstractions.Messaging;

namespace Application.Features.Platform.Plans.GetPlans;

public sealed record GetPlansQuery : IQuery<List<PlanResponse>>;

public sealed record PlanResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    string Currency,
    string? FeaturesJson,
    bool IsActive);
