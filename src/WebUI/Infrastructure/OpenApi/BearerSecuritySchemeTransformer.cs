using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WebUI.Infrastructure.OpenApi;

/// <summary>
/// Adds the JWT bearer security scheme to the OpenAPI document and requires it on every
/// operation (plan Phase 7a). The scheme is only applied when the JWT bearer
/// authentication handler is actually configured, so the document stays consistent
/// with the runtime pipeline.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        IEnumerable<AuthenticationScheme> schemes = await authenticationSchemeProvider.GetAllSchemesAsync();

        if (!schemes.Any(scheme => scheme.Name == JwtBearerDefaults.AuthenticationScheme))
        {
            return;
        }

        OpenApiComponents? components = document.Components;
        if (components is null)
        {
            components = new OpenApiComponents();
            document.Components = components;
        }

        IDictionary<string, IOpenApiSecurityScheme>? securitySchemes = components.SecuritySchemes;
        if (securitySchemes is null)
        {
            securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>();
            components.SecuritySchemes = securitySchemes;
        }

        securitySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT access token issued by POST /api/v1/auth/login."
        };

        if (document.Paths is null)
        {
            return;
        }

        foreach (IOpenApiPathItem path in document.Paths.Values)
        {
            if (path.Operations is null)
            {
                continue;
            }

            foreach (OpenApiOperation operation in path.Operations.Values)
            {
                if (operation.Security is null)
                {
                    operation.Security = [];
                }

                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            }
        }
    }
}
