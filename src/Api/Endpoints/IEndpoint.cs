using Asp.Versioning.Builder;

namespace Api.Endpoints;

public interface IEndpoint
{
    void MapEndpoints(IEndpointRouteBuilder app, ApiVersionSet apiVersionSet);
}
