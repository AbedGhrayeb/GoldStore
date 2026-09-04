using Application.Abstractions.Messaging;
using Application.Authorization.GetPermissions;
using Application.Authorization.GetRoles;
using Application.Authorization.GetUserPermissions;
using Application.Authorization.GetUserRoles;
using Application.Authorization.SetUserPermissions;
using Application.Authorization.SetUserRoles;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Admin-managed RBAC endpoints for tenant user roles/permissions.
/// </summary>
public sealed class AuthorizationEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}")
            .WithTags("Authorization")
            .RequireAuthorization()
            .RequireAuthorization($"feature:{Features.Settings}")
            .RequireAuthorization(policy => policy.RequireRole("store_admin"));

        group.MapGet("/roles", GetRoles)
            .WithSummary("List all roles with their permission keys (store_admin only).")
            .Produces<List<RoleResponse>>(StatusCodes.Status200OK);

        group.MapGet("/permissions", GetPermissions)
            .WithSummary("List all permissions (store_admin only).")
            .Produces<List<PermissionResponse>>(StatusCodes.Status200OK);

        group.MapGet("/users/{id:guid}/roles", GetUserRoles)
            .WithSummary("Get role IDs assigned to a user (store_admin only).")
            .Produces<List<Guid>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/users/{id:guid}/roles", SetUserRoles)
            .WithSummary("Replace roles for a user (store_admin only).")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/users/{id:guid}/permissions", GetUserPermissions)
            .WithSummary("Get effective permission keys for a user (store_admin only).")
            .Produces<List<string>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/users/{id:guid}/permissions", SetUserPermissions)
            .WithSummary("Replace direct permissions for a user (store_admin only).")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetRoles(IQueryDispatcher dispatcher, CancellationToken cancellationToken)
    {
        Result<List<RoleResponse>> result = await dispatcher.DispatchAsync<GetRolesQuery, List<RoleResponse>>(new GetRolesQuery(), cancellationToken);
        return ApiResults.From(result);
    }

    private static async Task<IResult> GetPermissions(IQueryDispatcher dispatcher, CancellationToken cancellationToken)
    {
        Result<List<PermissionResponse>> result = await dispatcher.DispatchAsync<GetPermissionsQuery, List<PermissionResponse>>(new GetPermissionsQuery(), cancellationToken);
        return ApiResults.From(result);
    }

    private static async Task<IResult> GetUserRoles(Guid id, IQueryDispatcher dispatcher, CancellationToken cancellationToken)
    {
        Result<List<Guid>> result = await dispatcher.DispatchAsync<GetUserRolesQuery, List<Guid>>(new GetUserRolesQuery(id), cancellationToken);
        return ApiResults.From(result);
    }

    private static async Task<IResult> SetUserRoles(Guid id, SetUserRolesRequest request, ICommandDispatcher dispatcher, CancellationToken cancellationToken)
    {
        Result<bool> result = await dispatcher.DispatchAsync<SetUserRolesCommand, bool>(new SetUserRolesCommand(id, request.RoleIds ?? []), cancellationToken);
        return ApiResults.From(result);
    }

    private static async Task<IResult> GetUserPermissions(Guid id, IQueryDispatcher dispatcher, CancellationToken cancellationToken)
    {
        Result<List<string>> result = await dispatcher.DispatchAsync<GetUserPermissionsQuery, List<string>>(new GetUserPermissionsQuery(id), cancellationToken);
        return ApiResults.From(result);
    }

    private static async Task<IResult> SetUserPermissions(Guid id, SetUserPermissionsRequest request, ICommandDispatcher dispatcher, CancellationToken cancellationToken)
    {
        Result<bool> result = await dispatcher.DispatchAsync<SetUserPermissionsCommand, bool>(new SetUserPermissionsCommand(id, request.PermissionKeys ?? []), cancellationToken);
        return ApiResults.From(result);
    }
}

public sealed record SetUserRolesRequest(List<Guid> RoleIds);
public sealed record SetUserPermissionsRequest(List<string> PermissionKeys);
