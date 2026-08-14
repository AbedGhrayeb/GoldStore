using System.Security.Claims;
using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Users.Login;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Builder;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Tenant-facing JWT authentication endpoints (plan Phase 7a). The Angular client and
/// external integrations obtain an access token here; tenant eligibility is enforced by
/// the login handler and the tenant resolution middleware. Replaces the former
/// <c>AuthController</c> surface at <c>/api/auth</c> â€” now served at
/// <c>/api/v1/auth</c>.
/// </summary>
public sealed class AuthEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/auth")
            .WithTags("Authentication");

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .RequireRateLimiting("LoginLimiter")
            .WithSummary("Sign in a store user and receive JWT access + refresh tokens.")
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", Refresh)
            .AllowAnonymous()
            .RequireRateLimiting("LoginLimiter")
            .WithSummary("Rotate the refresh token and issue a fresh access token.")
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", Logout)
            .AllowAnonymous()
            .WithSummary("Revoke the refresh token.")
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/me", Me)
            .RequireAuthorization()
            .WithSummary("Return the claims carried by the current bearer token.");
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        ICommandDispatcher dispatcher,
        ITokenProvider tokenProvider,
        IRefreshTokenService refreshTokenService,
        CancellationToken cancellationToken)
    {
        Result<Guid> loginResult = await dispatcher.DispatchAsync<LoginUserCommand, Guid>(
            new LoginUserCommand(request.Email, request.Password, null), cancellationToken);

        if (!loginResult.IsSuccess)
        {
            return ApiResults.UnauthorizedFrom(loginResult);
        }

        AccessTokenResponse access = await tokenProvider.CreateAccessTokenAsync(loginResult.Value, cancellationToken);
        RefreshTokenResponse refresh = await refreshTokenService.IssueAsync(loginResult.Value, cancellationToken);

        return TypedResults.Ok(new TokenResponse(access.Value, refresh.Value, refresh.ExpiresAtUtc));
    }

    private static async Task<IResult> Refresh(
        RefreshTokenRequest request,
        IRefreshTokenService refreshTokenService,
        ITokenProvider tokenProvider,
        CancellationToken cancellationToken)
    {
        Result<RefreshTokenResponse> refreshResult = await refreshTokenService.RotateAsync(
            request.RefreshToken, cancellationToken);

        if (!refreshResult.IsSuccess)
        {
            return ApiResults.UnauthorizedFrom(refreshResult);
        }

        AccessTokenResponse access = await tokenProvider.CreateAccessTokenAsync(
            refreshResult.Value.UserId, cancellationToken);

        return TypedResults.Ok(new TokenResponse(
            access.Value, refreshResult.Value.Value, refreshResult.Value.ExpiresAtUtc));
    }

    private static async Task<IResult> Logout(
        RefreshTokenRequest request,
        IRefreshTokenService refreshTokenService,
        CancellationToken cancellationToken)
    {
        await refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);
        return TypedResults.NoContent();
    }

    private static IResult Me(HttpContext context)
    {
        ClaimsPrincipal user = context.User;

        return TypedResults.Ok(new
        {
            userId = user.FindFirstValue(ClaimTypes.NameIdentifier),
            email = user.FindFirstValue(ClaimTypes.Email),
            tenantId = user.FindFirstValue(CustomClaims.TenantId),
            tenantKey = user.FindFirstValue(CustomClaims.TenantKey),
            roles = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value),
            permissions = user.FindAll(CustomClaims.Permission).Select(claim => claim.Value),
        });
    }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTimeOffset RefreshExpiresAt);
