using Application;
using Infrastructure;
using Infrastructure.Data;
using Serilog;
using WebUI;
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
                .AddInfrastructure(builder.Configuration);

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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                //app.ApplyMigrations();
            }

            // Route exceptions through the registered IExceptionHandler
            // (GlobalExceptionHandler) so tenant failures return consistent ProblemDetails.
            app.UseExceptionHandler();

            app.UseHttpsRedirection();

            if (app.Environment.IsDevelopment())
            {
                await app.InitializeDatabaseAsync();
            }

            //app.MapHealthChecks("health", new HealthCheckOptions
            //{
            //    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            //});

            app.UseRequestContextLogging();

            app.UseSerilogRequestLogging();

            if (!app.Environment.IsDevelopment())
            {
                app.UseResponseCompression();
            }
            app.UseCors("AllowAngularApp");

            app.UseRouting();
            app.MapStaticAssets();
            app.UseAuthentication();

            // Resolve and enforce the ambient tenant before authorization and
            // controllers run (plan Phase 2). Exemptions are explicit.
            app.UseTenantResolution();

            app.UseAuthorization();

            // REMARK: If you want to use Controllers, you'll need this.
            app.MapControllers();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            await app.RunAsync();
        }
    }
}
// REMARK: Required for functional and integration tests to work.
namespace WebUI
{
    public partial class Program;
}
