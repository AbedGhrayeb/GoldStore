// <copyright file="UsersEndpoints.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Users;
using Application.Users.Create;
using Application.Users.Delete;
using Application.Users.GetAllUsers;
using Application.Users.GetById;
using Application.Users.Update;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Tenant user management endpoints (plan Phase 7a, A6-B1). Every route dispatches the
/// same commands and queries the MVC <c>UsersController</c> uses. The tenant always comes
/// from the bearer token claims, never from a request parameter, so the <c>/me</c> profile
/// needs no id in the route.
/// </summary>
public sealed class UsersEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/users")
            .WithTags("Users")
            .RequireAuthorization()
            .RequireAuthorization($"feature:{Features.Settings}");

        group.MapGet("/", GetUsers)
            .RequireAuthorization(policy => policy.RequireRole("store_admin"))
            .WithSummary("List the store's users (store admin only).")
            .Produces<List<UserResponse>>(StatusCodes.Status200OK);

        group.MapGet("/me", GetMe)
            .WithSummary("Return the current user's profile.")
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateUser)
            .RequireAuthorization(policy => policy.RequireRole("store_admin"))
            .WithSummary("Create a new store user (store admin only).")
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}", UpdateUser)
            .WithSummary("Update a store user (self-only enforced).")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteUser)
            .RequireAuthorization(policy => policy.RequireRole("store_admin"))
            .WithSummary("Delete a store user (store admin only).")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetUsers(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<List<UserResponse>> result = await dispatcher.DispatchAsync<GetUsersQuery, List<UserResponse>>(
            new GetUsersQuery(), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetMe(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<UserResponse> result = await dispatcher.DispatchAsync<GetUserByIdQuery, UserResponse>(
            new GetUserByIdQuery(), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> CreateUser(
        CreateUserRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<CreateUserCommand, Guid>(
            new CreateUserCommand(request.Email, request.FirstName, request.LastName, request.Password, request.PhoneNumber, request.WhatsappNumber),
            cancellationToken);

        return ApiResults.Created($"/{ApiRoutes.Tenant}/users/{result.Value}", result);
    }

    private static async Task<IResult> UpdateUser(
        Guid id,
        UpdateUserRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await dispatcher.DispatchAsync<UpdateUserCommand, bool>(
            new UpdateUserCommand(id, request.FirstName, request.LastName, request.Password, request.PhoneNumber, request.WhatsappNumber),
            cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> DeleteUser(
        Guid id,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await dispatcher.DispatchAsync<DeleteUserCommand, bool>(
            new DeleteUserCommand(id), cancellationToken);

        return ApiResults.From(result);
    }
}

public sealed record CreateUserRequest(string Email, string FirstName, string LastName, string Password, string? PhoneNumber = null, string? WhatsappNumber = null);

public sealed record UpdateUserRequest(string FirstName, string LastName, string? Password = null, string? PhoneNumber = null, string? WhatsappNumber = null);
