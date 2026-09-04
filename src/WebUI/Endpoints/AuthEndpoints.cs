using System.Security.Claims;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Phone;
using Application.Features.Users.TwoFactor;
using Application.Users.Login;
using Domain.Users;
using Infrastructure.Authentication;
using Infrastructure.Phone;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel.Result;
using WebUI.Extensions;
using WebUI.Infrastructure.Authentication;

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
            .WithTags("Authentication")
            .ProducesProblem(StatusCodes.Status500InternalServerError);

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
            .WithSummary("Return the claims carried by the current bearer token.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // Firebase config for Angular (public)
        group.MapGet("/config/firebase", GetFirebaseConfig)
            .AllowAnonymous()
            .WithSummary("Return Firebase web config for phone auth.")
            .Produces<FirebaseConfigResponse>(StatusCodes.Status200OK);

        // Mandatory phone 2FA — setup requires tempTicket (enrollment on first login) or auth
        group.MapPost("/2fa/phone/setup", SetupPhone2fa)
            .AllowAnonymous()
            .RequireRateLimiting("LoginLimiter")
            .WithSummary("Verify phone idToken and enable mandatory 2FA (returns recovery codes).")
            .Produces<SetupPhone2faResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/2fa/phone/verify", VerifyPhone2fa)
            .AllowAnonymous()
            .RequireRateLimiting("LoginLimiter")
            .WithSummary("Second factor: verify Firebase idToken using tempTicket, issue tokens.")
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/2fa/phone/verify-recovery", VerifyPhoneRecovery)
            .AllowAnonymous()
            .RequireRateLimiting("LoginLimiter")
            .WithSummary("Second factor via recovery code.")
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/forgot-password/phone", ForgotPasswordPhone)
            .AllowAnonymous()
            .RequireRateLimiting("LoginLimiter")
            .WithSummary("Reset password via Firebase phone proof (no email).")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/2fa/phone/status", GetPhoneStatus)
            .RequireAuthorization()
            .WithSummary("Return 2FA / phone verification status for current user.")
            .Produces<PhoneStatusResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        ICommandDispatcher dispatcher,
        ITokenProvider tokenProvider,
        IRefreshTokenService refreshTokenService,
        JwtCookieManager cookieManager,
        IApplicationDbContext dbContext,
        ITwoFactorTicketService ticketService,
        IOptions<TwoFactorOptions> twoFactorOptions,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        Result<Guid> loginResult = await dispatcher.DispatchAsync<LoginUserCommand, Guid>(
            new LoginUserCommand(request.Email, request.Password, null), cancellationToken);

        if (!loginResult.IsSuccess)
        {
            return ApiResults.UnauthorizedFrom(loginResult);
        }

        // Mandatory 2FA gate: if user not enrolled or enrolled, issue tempTicket instead of tokens.
        bool mandatory = twoFactorOptions.Value.Mandatory;
        if (mandatory)
        {
            User? userFor2fa = await dbContext.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == loginResult.Value, cancellationToken);

            if (userFor2fa is not null)
            {
                bool needsEnrollment = !userFor2fa.TwoFactorEnabled;
                bool needsVerification = userFor2fa.TwoFactorEnabled;

                if (needsEnrollment || needsVerification)
                {
                    string tempTicket = ticketService.CreateTicket(userFor2fa.Id, userFor2fa.TenantId, userFor2fa.Email, isHost: false);
                    string? masked = null;
                    if (!string.IsNullOrWhiteSpace(userFor2fa.PhoneNumber) && userFor2fa.PhoneNumber.Length > 4)
                    {
                        masked = string.Concat("***", userFor2fa.PhoneNumber[^4..]);
                    }

                    if (needsEnrollment)
                    {
                        return TypedResults.Json(new Login2faRequiredResponse(true, false, masked, tempTicket), statusCode: StatusCodes.Status202Accepted);
                    }
                    else
                    {
                        return TypedResults.Json(new Login2faRequiredResponse(false, true, masked, tempTicket), statusCode: StatusCodes.Status202Accepted);
                    }
                }
            }
        }

        AccessTokenResponse access = await tokenProvider.CreateAccessTokenAsync(loginResult.Value, cancellationToken);
        RefreshTokenResponse refresh = await refreshTokenService.IssueAsync(loginResult.Value, cancellationToken);
        cookieManager.SetTenantTokens(context, access, refresh);

        return TypedResults.Ok(new TokenResponse(access.Value, refresh.Value, refresh.ExpiresAtUtc));
    }

    private static IResult GetFirebaseConfig(IOptions<FirebaseOptions> options)
    {
        FirebaseOptions o = options.Value;
        return TypedResults.Ok(new FirebaseConfigResponse(o.ProjectId, o.WebApiKey, o.AuthDomain, o.AppId));
    }

    private static async Task<IResult> SetupPhone2fa(
        SetupPhone2faRequest request,
        ICommandDispatcher dispatcher,
        ITwoFactorTicketService ticketService,
        IApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TempTicket) || string.IsNullOrWhiteSpace(request.IdToken))
        {
            return TypedResults.Unauthorized();
        }

        if (!ticketService.TryValidateTicket(request.TempTicket, out Guid userId, out _, out bool isHost) || isHost)
        {
            return TypedResults.Unauthorized();
        }

        // Prevent phone hijack: ensure ticket user matches requested flow (no extra check needed — idToken binds phone, we verify E164 matches Firebase phone)
        Result<SetupPhoneTwoFactorResult> result = await dispatcher.DispatchAsync<SetupPhoneTwoFactorCommand, SetupPhoneTwoFactorResult>(
            new SetupPhoneTwoFactorCommand(userId, request.IdToken), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(new SetupPhone2faResponse(result.Value.PhoneNumber, result.Value.RecoveryCodes))
            : ApiResults.From(result);
    }

    private static async Task<IResult> VerifyPhone2fa(
        VerifyPhone2faRequest request,
        ICommandDispatcher dispatcher,
        ITokenProvider tokenProvider,
        IRefreshTokenService refreshTokenService,
        JwtCookieManager cookieManager,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<VerifyPhoneTwoFactorCommand, Guid>(
            new VerifyPhoneTwoFactorCommand(request.TempTicket, request.IdToken), cancellationToken);

        if (!result.IsSuccess)
        {
            return ApiResults.UnauthorizedFrom(result);
        }

        AccessTokenResponse access = await tokenProvider.CreateAccessTokenAsync(result.Value, cancellationToken);
        RefreshTokenResponse refresh = await refreshTokenService.IssueAsync(result.Value, cancellationToken);
        cookieManager.SetTenantTokens(context, access, refresh);
        return TypedResults.Ok(new TokenResponse(access.Value, refresh.Value, refresh.ExpiresAtUtc));
    }

    private static async Task<IResult> VerifyPhoneRecovery(
        VerifyRecoveryRequest request,
        ICommandDispatcher dispatcher,
        ITokenProvider tokenProvider,
        IRefreshTokenService refreshTokenService,
        JwtCookieManager cookieManager,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<VerifyPhoneWithRecoveryCodeCommand, Guid>(
            new VerifyPhoneWithRecoveryCodeCommand(request.TempTicket, request.RecoveryCode), cancellationToken);

        if (!result.IsSuccess)
        {
            return ApiResults.UnauthorizedFrom(result);
        }

        AccessTokenResponse access = await tokenProvider.CreateAccessTokenAsync(result.Value, cancellationToken);
        RefreshTokenResponse refresh = await refreshTokenService.IssueAsync(result.Value, cancellationToken);
        cookieManager.SetTenantTokens(context, access, refresh);
        return TypedResults.Ok(new TokenResponse(access.Value, refresh.Value, refresh.ExpiresAtUtc));
    }

    private static async Task<IResult> ForgotPasswordPhone(
        ForgotPasswordPhoneRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<ResetPasswordWithPhoneCommand, Guid>(
            new ResetPasswordWithPhoneCommand(request.EmailOrPhone, request.IdToken, request.NewPassword), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(new { message = "Password reset successful" })
            : ApiResults.From(result);
    }

    private static async Task<IResult> GetPhoneStatus(
        HttpContext context,
        IApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        string? uid = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(uid, out Guid userId))
        {
            return TypedResults.Unauthorized();
        }

        User? user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        string? masked = null;
        if (!string.IsNullOrWhiteSpace(user.PhoneNumber) && user.PhoneNumber.Length > 4)
        {
            masked = string.Concat("***", user.PhoneNumber[^4..]);
        }

        return TypedResults.Ok(new PhoneStatusResponse(user.TwoFactorEnabled, user.PhoneNumberVerified, masked));
    }

    private static async Task<IResult> Refresh(
        RefreshTokenRequest? request,
        IRefreshTokenService refreshTokenService,
        ITokenProvider tokenProvider,
        JwtCookieManager cookieManager,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        string? refreshToken = request?.RefreshToken
            ?? context.Request.Cookies[JwtCookieDefaults.TenantRefreshCookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return TypedResults.Unauthorized();
        }

        Result<RefreshTokenResponse> refreshResult = await refreshTokenService.RotateAsync(
            refreshToken, cancellationToken);

        if (!refreshResult.IsSuccess)
        {
            return ApiResults.UnauthorizedFrom(refreshResult);
        }

        AccessTokenResponse access = await tokenProvider.CreateAccessTokenAsync(
            refreshResult.Value.UserId, cancellationToken);
        cookieManager.SetTenantTokens(context, access, refreshResult.Value);

        return TypedResults.Ok(new TokenResponse(
            access.Value, refreshResult.Value.Value, refreshResult.Value.ExpiresAtUtc));
    }

    private static async Task<IResult> Logout(
        RefreshTokenRequest? request,
        IRefreshTokenService refreshTokenService,
        JwtCookieManager cookieManager,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        string? refreshToken = request?.RefreshToken
            ?? context.Request.Cookies[JwtCookieDefaults.TenantRefreshCookieName];
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await refreshTokenService.RevokeAsync(refreshToken, cancellationToken);
        }

        cookieManager.DeleteTenantTokens(context);
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
            permissions = user
                .FindAll(CustomClaims.Permission)
                .Select(claim => claim.Value)
                .Concat(user.FindAll(CustomClaims.Feature).Select(claim => claim.Value)),
        });
    }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string? RefreshToken);

public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTimeOffset RefreshExpiresAt);

public sealed record Login2faRequiredResponse(bool RequiresEnrollment, bool RequiresTwoFactor, string? MaskedPhone, string TempTicket);

public sealed record SetupPhone2faRequest(string TempTicket, string IdToken);
public sealed record SetupPhone2faResponse(string PhoneNumber, IReadOnlyList<string> RecoveryCodes);
public sealed record VerifyPhone2faRequest(string TempTicket, string IdToken);
public sealed record VerifyRecoveryRequest(string TempTicket, string RecoveryCode);
public sealed record ForgotPasswordPhoneRequest(string EmailOrPhone, string IdToken, string NewPassword);
public sealed record PhoneStatusResponse(bool TwoFactorEnabled, bool PhoneNumberVerified, string? MaskedPhone);
public sealed record FirebaseConfigResponse(string ProjectId, string WebApiKey, string AuthDomain, string AppId);
