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
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                //app.ApplyMigrations();
            }

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

            // UseExceptionHandler is already configured above for non-development environments:
            // app.UseExceptionHandler("/Home/Error");
            // Do not call the parameterless overload here because it requires configuration in services.
            app.UseRouting();
            app.MapStaticAssets();
            app.UseAuthentication();

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
