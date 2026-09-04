using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Platform.Plans.GetPlans;

internal sealed class GetPlansQueryHandler(IPlatformDbContext platform)
    : IQueryHandler<GetPlansQuery, List<PlanResponse>>
{
    public async Task<Result<List<PlanResponse>>> Handle(GetPlansQuery query, CancellationToken cancellationToken)
    {
        var plans = await platform.Plans
            .AsNoTracking()
            .OrderBy(p => p.MonthlyPrice)
            .ToListAsync(cancellationToken);

        return plans
            .Select(p => new PlanResponse(
                p.Id,
                p.Name,
                p.Description,
                p.MonthlyPrice,
                p.AnnualPrice,
                p.Currency.ToCurrencyString(),
                p.FeaturesJson,
                p.IsActive))
            .ToList();
    }
}
