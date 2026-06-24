using Application;
using HealthChecks.UI.Client;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using WebUI;
using WebUI.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
builder.Services
    .AddApplication()
    .AddPresentation()
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

app.UseResponseCompression();
app.UseCors("AllowAngularApp");

app.UseExceptionHandler();
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

// REMARK: Required for functional and integration tests to work.
namespace WebUI
{
    public partial class Program;
}
