using System.Reflection;
using Api;
using Application;
using Infrastructure;
using Scalar.AspNetCore;

using Serilog;
using Web.Api.Extensions;
var builder = WebApplication.CreateBuilder(args);

//Add services to the container.
builder.Services
    .AddPresentation(builder.Configuration).AddApplication().AddInfrastructure(builder.Configuration);

builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Gold Store API V1");
        options.EnableDeepLinking();
        options.DisplayRequestDuration();
        options.EnableFilter();
    });
    app.MapScalarApiReference();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseHsts();
}
app.UseCoreMiddlewares(builder.Configuration);

app.UseAntiforgery();
app.MapGet("api/values", () => Results.Ok(new[] { "v1", "V2" }));
app.MapEndpoints();
app.Run();
