using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.PlatformUsers.Login;
using Application.Tenants.Provision;
using Application.Tenants.Reconciliation;
using Application.Tenants.UpdateStatus;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;
using WebUI.Authorization;
using WebUI.Extensions;

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

        group.MapGet("/auth/login", LoginPage)
            .AllowAnonymous()
            .WithSummary("Render the host sign-in page (browser cookie-challenge redirect target).")
            .Produces(StatusCodes.Status200OK);

        group.MapPost("/auth/login", Login)
            .AllowAnonymous()
            .RequireRateLimiting("LoginLimiter")
            .WithSummary("Sign in a platform user through the host cookie scheme.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/auth/logout", Logout)
            .RequireAuthorization()
            .WithSummary("Sign out the current platform user.")
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/tenants", GetTenants)
            .RequireAuthorization()
            .WithSummary("List all tenants.")
            .Produces<List<TenantSummaryResponse>>(StatusCodes.Status200OK);

        group.MapPost("/tenants", ProvisionTenant)
            .RequireAuthorization()
            .WithSummary("Provision a new tenant with settings, subscription, and admin.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPatch("/tenants/{tenantId:guid}/status", UpdateTenantStatus)
            .RequireAuthorization()
            .WithSummary("Move a tenant between trial, active, and cancelled.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/reconciliation", Reconciliation)
            .RequireAuthorization()
            .WithSummary("Reconcile tenant-owned rows across the database.")
            .Produces<TenantReconciliationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static IResult LoginPage(HttpRequest request)
    {
        string returnUrl = request.Query["ReturnUrl"].FirstOrDefault() ?? "/host";
        string html = $$"""
            <!doctype html>
            <html lang="ar" dir="rtl">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>دخول الإدارة العامة — GoldStore</title>
              <style>
                :root { --gold:#D4AF37; --gold-soft:#FFE088; --bg:#FCFAFA; --text:#1F2937; --error:#EF4444; }
                * { box-sizing:border-box; }
                body { margin:0; font-family:"IBM Plex Sans Arabic","Segoe UI",system-ui,sans-serif; background:var(--bg); color:var(--text); display:flex; align-items:center; justify-content:center; min-height:100vh; }
                .card { background:#FFF; border-radius:8px; box-shadow:0 4px 24px rgba(0,0,0,.08); padding:24px; width:100%; max-width:360px; border-top:4px solid var(--gold); }
                h1 { font-size:1.25rem; margin:0 0 4px; }
                p { color:#6B7280; margin:0 0 20px; font-size:.9rem; }
                label { display:block; font-size:.85rem; margin:0 0 6px; }
                input { width:100%; padding:10px 12px; border:1px solid #D1D5DB; border-radius:4px; font:inherit; margin-bottom:16px; }
                input:focus { outline:none; border-color:var(--gold); box-shadow:0 0 0 3px rgba(212,175,55,.25); }
                button { width:100%; background:var(--gold); color:#1F2937; font:inherit; font-weight:600; padding:11px; border:0; border-radius:4px; cursor:pointer; }
                button:disabled { opacity:.6; cursor:wait; }
                .error { display:none; background:#FEF2F2; color:var(--error); border:1px solid #FECACA; border-radius:4px; padding:10px 12px; font-size:.85rem; margin-bottom:16px; }
              </style>
            </head>
            <body>
              <div class="card">
                <h1>دخول الإدارة العامة</h1>
                <p>منصة GoldStore — إدارة المتاجر والاشتراكات</p>
                <div class="error" id="error"></div>
                <form id="login">
                  <label for="email">البريد الإلكتروني</label>
                  <input id="email" name="email" type="email" required autocomplete="username" />
                  <label for="password">كلمة المرور</label>
                  <input id="password" name="password" type="password" required autocomplete="current-password" />
                  <button type="submit">تسجيل الدخول</button>
                </form>
              </div>
              <script>
                const returnUrl = new URLSearchParams(location.search).get('ReturnUrl') || '/host';
                document.getElementById('login').addEventListener('submit', async (event) => {
                  event.preventDefault();
                  const errorBox = document.getElementById('error');
                  const button = event.target.querySelector('button');
                  button.disabled = true;
                  errorBox.style.display = 'none';
                  try {
                    const response = await fetch('/host/api/v1/auth/login', {
                      method: 'POST',
                      headers: { 'Content-Type': 'application/json' },
                      body: JSON.stringify({
                        email: document.getElementById('email').value,
                        password: document.getElementById('password').value,
                      }),
                    });
                    if (response.ok) {
                      window.location.assign(returnUrl);
                      return;
                    }
                    const payload = await response.json().catch(() => null);
                    errorBox.textContent = payload?.detail ?? 'تعذّر تسجيل الدخول. تحقّق من البيانات وحاول مجدداً.';
                  } finally {
                    button.disabled = false;
                  }
                  errorBox.style.display = 'block';
                });
              </script>
            </body>
            </html>
            """;
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        ICommandDispatcher dispatcher,
        IPlatformAuthSessionManager sessionManager,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<LoginPlatformUserCommand, Guid>(
            new LoginPlatformUserCommand(request.Email, request.Password), cancellationToken);

        if (!result.IsSuccess)
        {
            return ApiResults.UnauthorizedFrom(result);
        }

        await sessionManager.SignInAsync(request.Email, rememberMe: false, cancellationToken);

        return TypedResults.Ok(new { message = "Signed in" });
    }

    private static async Task<IResult> Logout(IPlatformAuthSessionManager sessionManager)
    {
        await sessionManager.SignOutAsync();
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
