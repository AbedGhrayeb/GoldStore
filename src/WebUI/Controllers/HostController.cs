using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.PlatformUsers.Login;
using Application.Tenants.Provision;
using Application.Tenants.UpdateStatus;
using Domain.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;
using WebUI.Authorization;

namespace WebUI.Controllers;

/// <summary>
/// Dedicated host administration endpoints (plan Phase 4 item 6). These authenticate a
/// <c>PlatformUser</c> through the host cookie scheme and are explicitly exempt from tenant
/// resolution via <see cref="HostOnlyAttribute"/>. A customer role can never reach them.
/// </summary>
[ApiController]
[Route("host")]
[HostOnly]
public sealed class HostController(
    ICommandHandler<LoginPlatformUserCommand, Guid> loginCommandHandler,
    IPlatformAuthSessionManager sessionManager,
    ICommandHandler<ProvisionTenantCommand, Guid> provisionTenantCommandHandler,
    ICommandHandler<UpdateTenantStatusCommand, Guid> updateTenantStatusCommandHandler,
    IApplicationDbContext context) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        Result<Guid> result = await loginCommandHandler.Handle(
            new LoginPlatformUserCommand(request.Email, request.Password), cancellationToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = result.TopError.Description });
        }

        await sessionManager.SignInAsync(request.Email, rememberMe: false, cancellationToken);

        return Ok(new { message = "Signed in" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await sessionManager.SignOutAsync();
        return NoContent();
    }

    [HttpGet("tenants")]
    [Authorize]
    public async Task<IActionResult> Tenants(CancellationToken cancellationToken)
    {
        List<TenantSummaryResponse> tenants = await context.Tenants
            .AsNoTracking()
            .OrderBy(tenant => tenant.Name)
            .Select(tenant => new TenantSummaryResponse(tenant.Id, tenant.Key, tenant.Name, tenant.Status.ToString()))
            .ToListAsync(cancellationToken);

        return Ok(tenants);
    }

    [HttpPost("tenants")]
    [Authorize]
    public async Task<IActionResult> ProvisionTenant([FromBody] ProvisionTenantRequest request, CancellationToken cancellationToken)
    {
        Result<Guid> result = await provisionTenantCommandHandler.Handle(new ProvisionTenantCommand(
            request.Name,
            request.Key,
            request.TimeZoneId,
            request.AdminFirstName,
            request.AdminLastName,
            request.AdminEmail,
            request.AdminPassword,
            request.SubscriptionPlanId,
            request.BillingCycle,
            request.StartsAtUtc,
            request.EndsAtUtc), cancellationToken);

        return result.IsSuccess ? Ok(new { tenantId = result.Value }) : BadRequest(new { message = result.TopError.Description });
    }

    [HttpPatch("tenants/{tenantId:guid}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateTenantStatus(Guid tenantId, [FromBody] UpdateTenantStatusRequest request, CancellationToken cancellationToken)
    {
        Result<Guid> result = await updateTenantStatusCommandHandler.Handle(
            new UpdateTenantStatusCommand(tenantId, request.NewStatus, request.TransitionAtUtc), cancellationToken);

        return result.IsSuccess ? Ok(new { tenantId = result.Value }) : BadRequest(new { message = result.TopError.Description });
    }
}

public sealed record TenantSummaryResponse(Guid Id, string Key, string Name, string Status);

public sealed record ProvisionTenantRequest(
    string Name,
    string Key,
    string TimeZoneId,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminPassword,
    Guid SubscriptionPlanId,
    SubscriptionBillingCycle BillingCycle,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc);

public sealed record UpdateTenantStatusRequest(TenantStatus NewStatus, DateTimeOffset? TransitionAtUtc);
