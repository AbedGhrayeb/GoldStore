namespace WebUI.Endpoints;

/// <summary>
/// An endpoint group (plan Phase 7a). Each group owns the HTTP surface of one module
/// and maps minimal API endpoints that dispatch the same commands, queries, validators,
/// and handlers the MVC layer uses. Groups are discovered and mapped automatically by
/// <c>MapEndpoints()</c>, so Program.cs never changes when a new group is added.
/// </summary>
public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
