using System.Text.Json;
using Application.Abstractions.Messaging;
using Application.Features.TenantProfile.GetTenantProfile;
using Application.Features.TenantSettings.GetTenantSettings;
using Application.Features.TenantSettings.UpdateTenantSettings;
using Application.Features.TenantSubscription.RenewSubscription;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;

namespace WebUI.ApiControllers.Tenant;

[Route("api/tenant")]
public sealed class TenantController(
    IQueryHandler<GetTenantProfileQuery, TenantProfileResponse> profileHandler,
    IQueryHandler<GetTenantSettingsQuery, JsonElement> settingsHandler,
    ICommandHandler<UpdateTenantSettingsCommand, Updated> updateSettingsHandler,
    ICommandHandler<RenewSubscriptionCommand, RenewSubscriptionResponse> renewSubscriptionHandler)
    : BaseApiController
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(TenantProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Returns the current tenant's profile, plan and subscription.")]
    [EndpointDescription("Reads the tenant catalog for the tenant resolved from the request subdomain.")]
    [EndpointName("GetTenantProfile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        Result<TenantProfileResponse> result = await profileHandler.Handle(new GetTenantProfileQuery(), cancellationToken);

        return result.Match(
            Ok,
            Problem);
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(JsonElement), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Returns the current tenant's settings JSON object.")]
    [EndpointName("GetTenantSettings")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        Result<JsonElement> result = await settingsHandler.Handle(new GetTenantSettingsQuery(), cancellationToken);

        return result.Match(
            settings => Ok(settings),
            Problem);
    }

    [HttpPut("settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Replaces the current tenant's settings JSON object.")]
    [EndpointDescription("Accepts any valid JSON object (max 16 KB) and stores it as the tenant's settings.")]
    [EndpointName("UpdateTenantSettings")]
    public async Task<IActionResult> UpdateSettings([FromBody] JsonElement settings, CancellationToken cancellationToken)
    {
        Result<Updated> result = await updateSettingsHandler.Handle(new UpdateTenantSettingsCommand(settings), cancellationToken);

        return result.Match(
            _ => NoContent(),
            Problem);
    }

    [HttpPost("subscription/renew")]
    [ProducesResponseType(typeof(RenewSubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Renews the tenant's subscription for the current plan.")]
    [EndpointDescription("Creates a new subscription period extending from the current expiry (or today); also exits read-only mode for expired tenants.")]
    [EndpointName("RenewSubscription")]
    public async Task<IActionResult> RenewSubscription([FromBody] RenewSubscriptionCommand command, CancellationToken cancellationToken)
    {
        Result<RenewSubscriptionResponse> result = await renewSubscriptionHandler.Handle(command, cancellationToken);

        return result.Match(
            Ok,
            Problem);
    }
}
