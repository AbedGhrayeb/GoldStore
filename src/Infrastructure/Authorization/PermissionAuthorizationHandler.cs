using Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Authorization;

/// <summary>
/// Grants a <see cref="PermissionRequirement"/> only when the authenticated principal
/// carries the matching permission claim. Claims are issued once at sign-in / token
/// creation, so authorization stays stateless (plan Phase 4 items 3 and 6).
/// </summary>
internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.User
                .FindAll(CustomClaims.Permission)
                .Any(claim => claim.Value.Equals(requirement.Permission, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
