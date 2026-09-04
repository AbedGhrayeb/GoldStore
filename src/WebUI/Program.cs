using Application;
using HealthChecks.UI.Client;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using Serilog;
using WebUI;
using WebUI.Endpoints;
using WebUI.Extensions;

namespace WebUI
{
    public partial class Program
    {
        public static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
            builder.Services
                .AddApplication()
                .AddPresentation(builder.Configuration)
                .AddInfrastructure(builder.Configuration)
                .AddEndpoints();

            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.MimeTypes = new[]
                {
                    "text/css",
                    "application/javascript",
                    "text/javascript",
                    "application/json",
                    "text/html",
                    "text/plain",
                    "image/svg+xml",
                    "font/woff2"
                };
            });

            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnet-hsts.
                app.UseHsts();
            }

            // Route exceptions through the registered IExceptionHandler
            // (GlobalExceptionHandler) so tenant failures return consistent ProblemDetails.
            app.UseExceptionHandler();

            // Trust only the deployment reverse proxy (nginx on loopback, see
            // deploy/nginx/nginx.conf) for X-Forwarded-* so IP-partitioned rate limiting
            // sees the real client address and HSTS/HTTPS redirect honor the proxy scheme.
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                KnownProxies = { System.Net.IPAddress.Loopback }
            });

            app.UseHttpsRedirection();

            // Migrate + seed on startup. Always in Development; in other environments only
            // when explicitly enabled via App:MigrateOnStartup (or the App__MigrateOnStartup
            // environment variable) so operators control when production DDL runs. Seeding is
            // idempotent, so the first boot after a fresh deploy is a safe no-op on re-runs.
            bool migrateOnStartup = app.Environment.IsDevelopment() ||
                app.Configuration.GetValue<bool>("App:MigrateOnStartup");

            if (migrateOnStartup)
            {
                await app.InitializeDatabaseAsync();
            }

            // Health probes: /health (all checks), /health/ready (database), /health/live
            // (process liveness — always healthy). These paths are exempt from tenant
            // resolution via Tenancy:ExemptPaths.
            app.MapHealthChecks("health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            app.MapHealthChecks("health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });
            app.MapHealthChecks("health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });

            app.UseRequestContextLogging();

            app.UseSerilogRequestLogging();

            if (!app.Environment.IsDevelopment())
            {
                app.UseResponseCompression();
            }
            app.UseCors("AllowAngularApp");

            app.UseRouting();

            // Rate limiting applies only to endpoints opted in via [EnableRateLimiting]
            // (login/refresh); health probes are never throttled.
            app.UseRateLimiter();

            app.UseAuthentication();

            // Resolve and enforce the ambient tenant before authorization and
            // endpoints run (plan Phase 2). Exemptions are explicit.
            app.UseTenantResolution();

            // Enforce mandatory phone 2FA after authentication (Firebase Phone Auth)
            app.UseTwoFactorEnforcement();

            app.UseAuthorization();

            // Auto-discovered minimal API endpoint groups (plan Phase 7a). Tenant
            // endpoints live under /api/v1 (JWT), host endpoints under /host/api/v1
            // (host cookie). Adding a new group never changes this file.
            app.MapEndpoints();

            // Built-in OpenAPI document + Scalar API reference UI (plan Phase 7a).
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            await app.RunAsync();
        }
    }
}
// REMARK: Required for functional and integration tests to work.
namespace WebUI
{
    public partial class Program;
}
