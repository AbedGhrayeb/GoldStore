using Application.Abstractions.Messaging;
using Application.Features.Platform.Tenants.ChangeTenantPlan;
using Application.Features.Platform.Tenants.ChangeTenantStatus;
using Application.Features.Platform.Tenants.GetTenants;
using Application.Features.Platform.Tenants.ProvisionTenant;
using Domain.Tenants;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;

namespace WebUI.ApiControllers.Platform;

[Route("api/platform/tenants")]
public sealed class PlatformTenantsController(
    ICommandHandler<ProvisionTenantCommand, Guid> provisionTenantHandler,
    IQueryHandler<GetTenantsQuery, List<TenantSummaryResponse>> getTenantsHandler,
    ICommandHandler<ChangeTenantStatusCommand, Updated> changeTenantStatusHandler,
    ICommandHandler<ChangeTenantPlanCommand, Updated> changeTenantPlanHandler)
    : BasePlatformApiController
{
    [HttpPost]
    [EndpointSummary("Provisions a new tenant (catalog entry + schema + seed data).")]
    public async Task<IActionResult> Provision([FromBody] ProvisionTenantCommand command, CancellationToken cancellationToken)
    {
        Result<Guid> result = await provisionTenantHandler.Handle(command, cancellationToken);

        return result.Match(
            id => Ok(new { id }),
            Problem);
    }

    [HttpGet]
    [EndpointSummary("Lists all tenants with their plan and status.")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        Result<List<TenantSummaryResponse>> result = await getTenantsHandler.Handle(new GetTenantsQuery(), cancellationToken);

        return result.Match(
            Ok,
            Problem);
    }

    [HttpPost("{id:guid}/suspend")]
    [EndpointSummary("Suspends a tenant — all its users are blocked immediately.")]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken cancellationToken)
    {
        Result<Updated> result = await changeTenantStatusHandler.Handle(
            new ChangeTenantStatusCommand(id, TenantStatus.Suspended), cancellationToken);

        return result.Match(
            _ => NoContent(),
            Problem);
    }

    [HttpPost("{id:guid}/reactivate")]
    [EndpointSummary("Reactivates a suspended tenant.")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken)
    {
        Result<Updated> result = await changeTenantStatusHandler.Handle(
            new ChangeTenantStatusCommand(id, TenantStatus.Active), cancellationToken);

        return result.Match(
            _ => NoContent(),
            Problem);
    }

    [HttpPost("{id:guid}/plan")]
    [EndpointSummary("Changes a tenant's plan and billing interval, extending the subscription period.")]
    public async Task<IActionResult> ChangePlan(Guid id, [FromBody] ChangeTenantPlanCommand command, CancellationToken cancellationToken)
    {
        Result<Updated> result = await changeTenantPlanHandler.Handle(command with { TenantId = id }, cancellationToken);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}
