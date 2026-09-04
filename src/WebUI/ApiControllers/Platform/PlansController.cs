using Application.Abstractions.Messaging;
using Application.Features.Platform.Plans.GetPlans;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;

namespace WebUI.ApiControllers.Platform;

[Route("api/platform/plans")]
public sealed class PlansController(
    IQueryHandler<GetPlansQuery, List<PlanResponse>> getPlansHandler)
    : BasePlatformApiController
{
    [HttpGet]
    [EndpointSummary("Lists active subscription plans.")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        Result<List<PlanResponse>> result = await getPlansHandler.Handle(new GetPlansQuery(), cancellationToken);

        return result.Match(
            Ok,
            Problem);
    }
}
