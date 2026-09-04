using Application.Abstractions.Messaging;
using Application.Features.Platform.Auth.PlatformAdminLogin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;

namespace WebUI.ApiControllers.Platform;

[Route("api/platform/auth")]
public sealed class PlatformAuthController(ICommandHandler<PlatformAdminLoginCommand, PlatformAdminLoginResponse> loginHandler)
    : BasePlatformApiController
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(PlatformAdminLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Authenticates a platform administrator and returns an access token.")]
    [EndpointDescription("Validates the platform admin credentials and returns a JWT scoped to the platform audience.")]
    [EndpointName("PlatformAdminLogin")]
    public async Task<IActionResult> Login([FromBody] PlatformAdminLoginCommand command, CancellationToken cancellationToken)
    {
        Result<PlatformAdminLoginResponse> result = await loginHandler.Handle(command, cancellationToken);

        return result.Match(
            response => Ok(response),
            Problem);
    }
}
