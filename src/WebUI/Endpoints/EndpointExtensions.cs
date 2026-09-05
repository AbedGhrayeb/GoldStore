// <copyright file="EndpointExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace WebUI.Endpoints;

/// <summary>
/// Discovers and maps every <see cref="IEndpoint"/> implementation in the WebUI
/// assembly. <c>Program.cs</c> calls <c>AddEndpoints()</c> during composition and
/// <c>MapEndpoints()</c> once in the pipeline; adding a new endpoint group never
/// touches either file.
/// </summary>
public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        IEnumerable<Type> endpointTypes = typeof(EndpointExtensions).Assembly
            .GetTypes()
            .Where(type => !type.IsInterface
                && !type.IsAbstract
                && type.IsAssignableTo(typeof(IEndpoint)));

        foreach (Type endpointType in endpointTypes)
        {
            services.AddScoped(typeof(IEndpoint), endpointType);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        using IServiceScope scope = app.ServiceProvider.CreateScope();

        foreach (IEndpoint endpoint in scope.ServiceProvider.GetServices<IEndpoint>())
        {
            endpoint.Map(app);
        }

        return app;
    }
}
