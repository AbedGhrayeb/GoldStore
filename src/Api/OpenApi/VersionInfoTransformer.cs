using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Api.OpenApi;

internal sealed class VersionInfoTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var version = context.DocumentName;

        document.Info.Version = version;
        document.Info.Title = $"MechanicShop API {version}";

        return Task.CompletedTask;
    }
}
