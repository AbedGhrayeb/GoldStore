// <copyright file="TwoFactorEnforcementMiddleware.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Infrastructure.Authentication;
using Infrastructure.Phone;
using Microsoft.Extensions.Options;

namespace WebUI.Middleware;

public sealed class TwoFactorEnforcementMiddleware(RequestDelegate next, IOptions<TwoFactorOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!options.Value.Mandatory)
        {
            await next(context);
            return;
        }

        Endpoint? endpoint = context.GetEndpoint();
        if (endpoint is not null)
        {
            // Allow anonymous + phone 2FA + firebase config + health
            if (endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() is not null)
            {
                await next(context);
                return;
            }

            string path = context.Request.Path.Value ?? string.Empty;
            if (path.Contains("/2fa/phone", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/config/firebase", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/forgot-password/phone", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }
        }

        // Only enforce for authenticated users
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        // Temp ticket with purpose=2fa_pending must not reach normal endpoints
        if (context.User.HasClaim(c => c.Type == "purpose" && c.Value == "2fa_pending"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "TwoFactorRequired", message = "المصادقة الثنائية مطلوبة" });
            return;
        }

        // Host or tenant: require amr=mfa claim when mandatory
        bool hasMfa = context.User.HasClaim(c => c.Type == "amr" && c.Value == "mfa");
        if (!hasMfa)
        {
            // Check if user is tenant vs host; both need mfa
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "TwoFactorRequired", message = "يجب تفعيل المصادقة الثنائية عبر الهاتف (+970). الرجاء إكمال التوثيق." });
            return;
        }

        await next(context);
    }
}
