using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Phone;
using Application.Features.PlatformUsers.TwoFactor;
using Application.Features.SubscriptionPlans;
using Application.Features.SubscriptionPlans.Create;
using Application.Features.SubscriptionPlans.GetSubscriptionPlans;
using Application.Features.SubscriptionPlans.Update;
using Application.Features.Subscriptions;
using Application.Features.Subscriptions.GetTenantSubscription;
using Application.Features.Subscriptions.Renew;
using Application.PlatformUsers.Login;
using Application.Tenants.Provision;
using Application.Tenants.Reconciliation;
using Application.Tenants.UpdateStatus;
using Domain.Tenants;
using Infrastructure.Phone;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel.Result;
using WebUI.Authorization;
using WebUI.Extensions;
using WebUI.Infrastructure.Authentication;

namespace WebUI.Endpoints;

/// <summary>
/// Dedicated host administration endpoints (plan Phase 7a). These authenticate a
/// <c>PlatformUser</c> through the host cookie scheme and are explicitly exempt from
/// tenant resolution via <see cref="HostOnlyAttribute"/>. A customer role can never reach
/// them. Replaces the former <c>HostController</c> surface at <c>/host</c> — now served at
/// <c>/host/api/v1</c>.
/// </summary>
public sealed class HostEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Host}")
            .WithTags("Host Administration")
            .WithMetadata(new HostOnlyAttribute());

        group.MapPost("/auth/login", Login)
            .AllowAnonymous()
            .RequireRateLimiting("LoginLimiter")
            .WithSummary("Sign in a platform user and set an HttpOnly host JWT cookie.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/auth/me", Me)
            .RequireAuthorization()
            .WithSummary("Return the active host administrator identity.")
            .Produces<HostMeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/auth/logout", Logout)
            .RequireAuthorization()
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Sign out the current platform user.")
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/tenants", GetTenants)
            .RequireAuthorization()
            .WithSummary("List all tenants.")
            .Produces<List<TenantSummaryResponse>>(StatusCodes.Status200OK);

        group.MapPost("/tenants", ProvisionTenant)
            .RequireAuthorization()
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Provision a new tenant with settings, subscription, and admin.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPatch("/tenants/{tenantId:guid}/status", UpdateTenantStatus)
            .RequireAuthorization()
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Move a tenant between trial, active, and cancelled.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/reconciliation", Reconciliation)
            .RequireAuthorization()
            .WithSummary("Reconcile tenant-owned rows across the database.")
            .Produces<TenantReconciliationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        RouteGroupBuilder plansGroup = group.MapGroup("/subscription-plans");
        plansGroup.MapGet("/", GetSubscriptionPlans)
            .RequireAuthorization()
            .WithSummary("List all subscription plans.")
            .Produces<IReadOnlyList<SubscriptionPlanResponse>>(StatusCodes.Status200OK);
        plansGroup.MapPost("/", CreateSubscriptionPlan)
            .RequireAuthorization()
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Create a new subscription plan.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        plansGroup.MapPut("/{id:guid}", UpdateSubscriptionPlan)
            .RequireAuthorization()
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Update an existing subscription plan.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/tenants/{tenantId:guid}/subscription", GetTenantSubscription)
            .RequireAuthorization()
            .WithSummary("Get the current subscription for a tenant.")
            .Produces<TenantSubscriptionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/tenants/{tenantId:guid}/subscription/renew", RenewTenantSubscription)
            .RequireAuthorization()
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Renew or change a tenant subscription.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Host Firebase config + mandatory phone 2FA
        group.MapGet("/auth/config/firebase", HostFirebaseConfig)
            .AllowAnonymous()
            .WithSummary("Return Firebase web config for host phone auth.")
            .Produces<FirebaseConfigResponse>(StatusCodes.Status200OK);

        group.MapPost("/auth/2fa/phone/setup", HostSetupPhone2fa)
            .AllowAnonymous()
            .RequireRateLimiting("LoginLimiter")
            .WithSummary("Host: verify phone idToken and enable mandatory 2FA.")
            .Produces<SetupPhone2faResponse>(StatusCodes.Status200OK);

        group.MapPost("/auth/2fa/phone/verify", HostVerifyPhone2fa)
            .AllowAnonymous()
            .RequireRateLimiting("LoginLimiter")
            .WithSummary("Host: second factor verify.")
            .Produces(StatusCodes.Status200OK);

        group.MapPost("/auth/2fa/phone/verify-recovery", HostVerifyRecovery)
            .AllowAnonymous()
            .RequireRateLimiting("LoginLimiter")
            .WithSummary("Host: second factor via recovery code.");

        group.MapPost("/auth/forgot-password/phone", HostForgotPasswordPhone)
            .AllowAnonymous()
            .RequireRateLimiting("LoginLimiter")
            .WithSummary("Host: reset password via phone idToken.")
            .Produces(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        ICommandDispatcher dispatcher,
        IPlatformTokenProvider tokenProvider,
        JwtCookieManager cookieManager,
        IAntiforgery antiforgery,
        IApplicationDbContext dbContext,
        ITwoFactorTicketService ticketService,
        IOptions<TwoFactorOptions> twoFactorOptions,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<LoginPlatformUserCommand, Guid>(
            new LoginPlatformUserCommand(request.Email, request.Password), cancellationToken);

        if (!result.IsSuccess)
        {
            return ApiResults.UnauthorizedFrom(result);
        }

        if (twoFactorOptions.Value.Mandatory)
        {
            PlatformUser? pu = await dbContext.PlatformUsers.AsNoTracking().SingleOrDefaultAsync(u => u.Id == result.Value, cancellationToken);
            if (pu is not null)
            {
                bool needsEnroll = !pu.TwoFactorEnabled;
                bool needsVerify = pu.TwoFactorEnabled;
                if (needsEnroll || needsVerify)
                {
                    string tempTicket = ticketService.CreateTicket(pu.Id, null, pu.Email, isHost: true);
                    string? masked = null;
                    if (!string.IsNullOrWhiteSpace(pu.PhoneNumber) && pu.PhoneNumber.Length > 4)
                    {
                        masked = string.Concat("***", pu.PhoneNumber[^4..]);
                    }

                    if (needsEnroll)
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

        PlatformAccessTokenResponse accessToken = await tokenProvider.CreateAccessTokenAsync(result.Value, cancellationToken);
        cookieManager.SetHostToken(context, accessToken);
        // Antiforgery tokens are tied to the current user identity. The login request itself is
        // anonymous, so we must project the just-authenticated host identity into HttpContext.User
        // before generating tokens; otherwise the token's embedded username (empty) will mismatch
        // the host principal (PlatformUser email/name) on subsequent PUT/PATCH validations and
        // produce "was meant for a different claims-based user".
        Domain.Tenants.PlatformUser? platformUser = await dbContext.PlatformUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == result.Value, cancellationToken);
        if (platformUser is not null)
        {
            System.Security.Claims.ClaimsIdentity hostIdentity = new(
            [
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, $"{platformUser.FirstName} {platformUser.LastName}".Trim()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, platformUser.Id.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, platformUser.Email),
            ], "Host");
            context.User = new System.Security.Claims.ClaimsPrincipal(hostIdentity);
        }
        AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.Strict,
            Secure = context.Request.IsHttps,
            IsEssential = true,
            Path = "/"
        });

        return TypedResults.Ok(new { message = "Signed in" });
    }

    private static IResult Me(HttpContext context) =>
        TypedResults.Ok(new HostMeResponse(
            context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty));

    private static IResult Logout(JwtCookieManager cookieManager, HttpContext context)
    {
        cookieManager.DeleteHostToken(context);
        context.Response.Cookies.Delete("XSRF-TOKEN", new CookieOptions { Path = "/" });
        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetTenants(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        List<TenantSummaryResponse> tenants = await context.Tenants
            .AsNoTracking()
            .OrderBy(tenant => tenant.Name)
            .Select(tenant => new TenantSummaryResponse(tenant.Id, tenant.Key, tenant.Name, tenant.Status.ToString()))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(tenants);
    }

    private static async Task<IResult> ProvisionTenant(
        ProvisionTenantRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<ProvisionTenantCommand, Guid>(
            new ProvisionTenantCommand(
                request.Name,
                request.Key,
                request.TimeZoneId,
                request.AdminFirstName,
                request.AdminLastName,
                request.AdminEmail,
                request.AdminPassword,
                request.AdminPhoneNumber,
                request.AdminWhatsappNumber,
                request.SubscriptionPlanId,
                request.BillingCycle,
                request.StartsAtUtc,
                request.EndsAtUtc),
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(new { tenantId = result.Value })
            : ApiResults.From(result);
    }

    private static async Task<IResult> UpdateTenantStatus(
        Guid tenantId,
        UpdateTenantStatusRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<UpdateTenantStatusCommand, Guid>(
            new UpdateTenantStatusCommand(tenantId, request.NewStatus, request.TransitionAtUtc),
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(new { tenantId = result.Value })
            : ApiResults.From(result);
    }

    private static async Task<IResult> Reconciliation(
        Guid? tenantId,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<TenantReconciliationResponse> result = await dispatcher.DispatchAsync<GetTenantReconciliationQuery, TenantReconciliationResponse>(
            new GetTenantReconciliationQuery(tenantId), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : ApiResults.From(result);
    }

    private static async Task<IResult> GetSubscriptionPlans(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<SubscriptionPlanResponse>> result = await dispatcher.DispatchAsync<GetSubscriptionPlansQuery, IReadOnlyList<SubscriptionPlanResponse>>(
            new GetSubscriptionPlansQuery(), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : ApiResults.From(result);
    }

    private static async Task<IResult> CreateSubscriptionPlan(
        CreateSubscriptionPlanRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<CreateSubscriptionPlanCommand, Guid>(
            new CreateSubscriptionPlanCommand(
                request.Name,
                request.Key,
                request.MaximumActiveUsers,
                request.MaximumPostedInvoicesPerPeriod,
                request.MaximumActiveBranches,
                request.MaximumStorageBytes,
                request.IsTrial,
                request.DurationInMonths,
                request.Price,
                request.DiscountPercent),
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(new { planId = result.Value })
            : ApiResults.From(result);
    }

    private static async Task<IResult> UpdateSubscriptionPlan(
        Guid id,
        UpdateSubscriptionPlanRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<UpdateSubscriptionPlanCommand, Guid>(
            new UpdateSubscriptionPlanCommand(
                id,
                request.Name,
                request.Key,
                request.MaximumActiveUsers,
                request.MaximumPostedInvoicesPerPeriod,
                request.MaximumActiveBranches,
                request.MaximumStorageBytes,
                request.IsTrial,
                request.DurationInMonths,
                request.Price,
                request.DiscountPercent,
                request.IsActive),
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(new { planId = result.Value })
            : ApiResults.From(result);
    }

    private static async Task<IResult> GetTenantSubscription(
        Guid tenantId,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<TenantSubscriptionResponse> result = await dispatcher.DispatchAsync<GetTenantSubscriptionQuery, TenantSubscriptionResponse>(
            new GetTenantSubscriptionQuery(tenantId), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : ApiResults.From(result);
    }

    private static async Task<IResult> RenewTenantSubscription(
        Guid tenantId,
        RenewSubscriptionRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<RenewTenantSubscriptionCommand, Guid>(
            new RenewTenantSubscriptionCommand(
                tenantId,
                request.NewPlanId,
                request.BillingCycle,
                request.StartsAtUtc,
                request.EndsAtUtc),
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(new { subscriptionId = result.Value })
            : ApiResults.From(result);
    }

    private static IResult HostFirebaseConfig(IOptions<FirebaseOptions> options)
    {
        FirebaseOptions o = options.Value;
        return TypedResults.Ok(new FirebaseConfigResponse(o.ProjectId, o.WebApiKey, o.AuthDomain, o.AppId));
    }

    private static async Task<IResult> HostSetupPhone2fa(
        SetupPhone2faRequest request,
        ICommandDispatcher dispatcher,
        ITwoFactorTicketService ticketService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TempTicket) || string.IsNullOrWhiteSpace(request.IdToken))
        {
            return TypedResults.Unauthorized();
        }

        if (!ticketService.TryValidateTicket(request.TempTicket, out Guid userId, out _, out bool isHost) || !isHost)
        {
            return TypedResults.Unauthorized();
        }

        Result<SetupPlatformPhoneTwoFactorResult> result = await dispatcher.DispatchAsync<SetupPlatformPhoneTwoFactorCommand, SetupPlatformPhoneTwoFactorResult>(
            new SetupPlatformPhoneTwoFactorCommand(userId, request.IdToken), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(new SetupPhone2faResponse(result.Value.PhoneNumber, result.Value.RecoveryCodes))
            : ApiResults.From(result);
    }

    private static async Task<IResult> HostVerifyPhone2fa(
        VerifyPhone2faRequest request,
        ICommandDispatcher dispatcher,
        IPlatformTokenProvider tokenProvider,
        JwtCookieManager cookieManager,
        IAntiforgery antiforgery,
        IApplicationDbContext dbContext,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<VerifyPlatformPhoneTwoFactorCommand, Guid>(
            new VerifyPlatformPhoneTwoFactorCommand(request.TempTicket, request.IdToken), cancellationToken);
        if (!result.IsSuccess)
        {
            return ApiResults.UnauthorizedFrom(result);
        }

        PlatformAccessTokenResponse token = await tokenProvider.CreateAccessTokenAsync(result.Value, cancellationToken);
        cookieManager.SetHostToken(context, token);

        PlatformUser? pu = await dbContext.PlatformUsers.AsNoTracking().SingleOrDefaultAsync(u => u.Id == result.Value, cancellationToken);
        if (pu is not null)
        {
            var id = new System.Security.Claims.ClaimsIdentity([
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, $"{pu.FirstName} {pu.LastName}".Trim()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, pu.Id.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, pu.Email),
            ], "Host");
            context.User = new System.Security.Claims.ClaimsPrincipal(id);
        }
        AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Strict, Secure = context.Request.IsHttps, IsEssential = true, Path = "/" });
        return TypedResults.Ok(new { message = "Signed in" });
    }

    private static async Task<IResult> HostVerifyRecovery(
        VerifyRecoveryRequest request,
        ICommandDispatcher dispatcher,
        IPlatformTokenProvider tokenProvider,
        JwtCookieManager cookieManager,
        IAntiforgery antiforgery,
        IApplicationDbContext dbContext,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<VerifyPlatformWithRecoveryCodeCommand, Guid>(
            new VerifyPlatformWithRecoveryCodeCommand(request.TempTicket, request.RecoveryCode), cancellationToken);
        if (!result.IsSuccess)
        {
            return ApiResults.UnauthorizedFrom(result);
        }

        PlatformAccessTokenResponse token = await tokenProvider.CreateAccessTokenAsync(result.Value, cancellationToken);
        cookieManager.SetHostToken(context, token);
        PlatformUser? pu = await dbContext.PlatformUsers.AsNoTracking().SingleOrDefaultAsync(u => u.Id == result.Value, cancellationToken);
        if (pu is not null)
        {
            var id = new System.Security.Claims.ClaimsIdentity([
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, $"{pu.FirstName} {pu.LastName}".Trim()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, pu.Id.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, pu.Email),
            ], "Host");
            context.User = new System.Security.Claims.ClaimsPrincipal(id);
        }
        AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Strict, Secure = context.Request.IsHttps, IsEssential = true, Path = "/" });
        return TypedResults.Ok(new { message = "Signed in" });
    }

    private static async Task<IResult> HostForgotPasswordPhone(
        ForgotPasswordPhoneRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<ResetPlatformPasswordWithPhoneCommand, Guid>(
            new ResetPlatformPasswordWithPhoneCommand(request.EmailOrPhone, request.IdToken, request.NewPassword), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(new { message = "Password reset successful" }) : ApiResults.From(result);
    }
}

public sealed record TenantSummaryResponse(Guid Id, string Key, string Name, string Status);

public sealed record HostMeResponse(string Email);

public sealed record ProvisionTenantRequest(
    string Name,
    string Key,
    string TimeZoneId,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminPassword,
    string? AdminPhoneNumber,
    string? AdminWhatsappNumber,
    Guid SubscriptionPlanId,
    SubscriptionBillingCycle BillingCycle,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc);

public sealed record UpdateTenantStatusRequest(TenantStatus NewStatus, DateTimeOffset? TransitionAtUtc);

public sealed record CreateSubscriptionPlanRequest(
    string Name,
    string? Key,
    int? MaximumActiveUsers,
    int? MaximumPostedInvoicesPerPeriod,
    int? MaximumActiveBranches,
    long? MaximumStorageBytes,
    bool IsTrial,
    int DurationInMonths,
    decimal Price,
    decimal? DiscountPercent);

public sealed record UpdateSubscriptionPlanRequest(
    string Name,
    string? Key,
    int? MaximumActiveUsers,
    int? MaximumPostedInvoicesPerPeriod,
    int? MaximumActiveBranches,
    long? MaximumStorageBytes,
    bool IsTrial,
    int DurationInMonths,
    decimal Price,
    decimal? DiscountPercent,
    bool IsActive);

public sealed record RenewSubscriptionRequest(
    Guid? NewPlanId,
    SubscriptionBillingCycle BillingCycle,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc);
