using System.Security.Claims;
using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Users.Login;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;

namespace WebUI.Controllers;

/// <summary>
/// JWT authentication endpoints for API clients (plan Phase 4 items 1-5). The Angular
/// client and external integrations obtain an access token here; tenant eligibility is
/// enforced by the login handler and the tenant resolution middleware.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    ICommandHandler<LoginUserCommand, Guid> loginCommandHandler,
    ITokenProvider tokenProvider,
    IRefreshTokenService refreshTokenService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        Result<Guid> loginResult = await loginCommandHandler.Handle(
            new LoginUserCommand(request.Email, request.Password, null), cancellationToken);

        if (!loginResult.IsSuccess)
        {
            return Unauthorized(new { message = loginResult.TopError.Description });
        }

        AccessTokenResponse access = await tokenProvider.CreateAccessTokenAsync(loginResult.Value, cancellationToken);
        RefreshTokenResponse refresh = await refreshTokenService.IssueAsync(loginResult.Value, cancellationToken);

        return Ok(new TokenResponse(access.Value, refresh.Value, refresh.ExpiresAtUtc));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        Result<RefreshTokenResponse> refreshResult = await refreshTokenService.RotateAsync(request.RefreshToken, cancellationToken);

        if (!refreshResult.IsSuccess)
        {
            return Unauthorized(new { message = refreshResult.TopError.Description });
        }

        AccessTokenResponse access = await tokenProvider.CreateAccessTokenAsync(refreshResult.Value.UserId, cancellationToken);

        return Ok(new TokenResponse(access.Value, refreshResult.Value.Value, refreshResult.Value.ExpiresAtUtc));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email),
            tenantId = User.FindFirstValue(CustomClaims.TenantId),
            tenantKey = User.FindFirstValue(CustomClaims.TenantKey),
            roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value),
            permissions = User.FindAll(CustomClaims.Permission).Select(claim => claim.Value),
        });
    }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTimeOffset RefreshExpiresAt);
