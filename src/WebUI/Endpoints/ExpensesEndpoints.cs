using Application.Abstractions.Messaging;
using Application.Common.Models;
using Application.Features.Expenses.ExpenseCategories;
using Application.Features.Expenses.ExpenseCategories.Create;
using Application.Features.Expenses.ExpenseCategories.Delete;
using Application.Features.Expenses.ExpenseCategories.GetAll;
using Application.Features.Expenses.ExpenseCategories.Update;
using Application.Features.Expenses.Expenses;
using Application.Features.Expenses.Expenses.Create;
using Application.Features.Expenses.Expenses.Delete;
using Application.Features.Expenses.Expenses.GetKpis;
using Application.Features.Expenses.Expenses.GetPaged;
using Application.Features.Expenses.Expenses.Update;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using SharedKernel;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Tenant-scoped expense and expense-category endpoints.
/// </summary>
public sealed class ExpensesEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/expenses")
            .WithTags("Expenses")
            .RequireAuthorization()
            .RequireAuthorization($"feature:{Features.Expenses}")
            .RequireAuthorization("expenses.view")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/", GetExpenses)
            .WithSummary("Return paged expenses.")
            .Produces<PaginatedList<ExpenseResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet("/kpis", GetKpis)
            .WithSummary("Return expense KPIs.")
            .Produces<ExpenseKpiResponse>(StatusCodes.Status200OK);

        group.MapPost("/", CreateExpense)
                    .RequireAuthorization("expenses.manage")
            .WithSummary("Create an expense and its financial outflow.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateExpense)
                    .RequireAuthorization("expenses.manage")
            .WithSummary("Update an expense and its financial outflow.")
            .Produces<Updated>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteExpense)
                    .RequireAuthorization("expenses.manage")
            .WithSummary("Delete an expense and its financial outflow.")
            .Produces<Deleted>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/categories", GetCategories)
            .WithSummary("List expense categories.")
            .Produces<List<ExpenseCategoryResponse>>(StatusCodes.Status200OK);

        group.MapPost("/categories", CreateCategory)
                    .RequireAuthorization("expenses.manage")
            .WithSummary("Create an expense category.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/categories/{id:guid}", UpdateCategory)
                    .RequireAuthorization("expenses.manage")
            .WithSummary("Update an expense category.")
            .Produces<Updated>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/categories/{id:guid}", DeleteCategory)
                    .RequireAuthorization("expenses.manage")
            .WithSummary("Delete an unused expense category.")
            .Produces<Deleted>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> GetExpenses(
        int? page,
        int? pageSize,
        string? accountName,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? categoryId,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<PaginatedList<ExpenseResponse>> result = await dispatcher
            .DispatchAsync<GetExpensesQuery, PaginatedList<ExpenseResponse>>(
                new GetExpensesQuery(
                    page ?? 1,
                    pageSize ?? 20,
                    accountName,
                    fromDate,
                    toDate,
                    categoryId),
                cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetKpis(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<ExpenseKpiResponse> result = await dispatcher.DispatchAsync<GetExpenseKpisQuery, ExpenseKpiResponse>(
            new GetExpenseKpisQuery(), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> CreateExpense(
        CreateExpenseRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<CreateExpenseCommand, Guid>(
            new CreateExpenseCommand(
                request.ExpenseDate,
                request.CategoryId,
                request.Description,
                request.Amount,
                request.AccountId),
            cancellationToken);

        return ApiResults.Created(result);
    }

    private static async Task<IResult> UpdateExpense(
        Guid id,
        UpdateExpenseRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Updated> result = await dispatcher.DispatchAsync<UpdateExpenseCommand, Updated>(
            new UpdateExpenseCommand(
                id,
                request.ExpenseDate,
                request.CategoryId,
                request.Description,
                request.Amount,
                request.AccountId),
            cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> DeleteExpense(
        Guid id,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Deleted> result = await dispatcher.DispatchAsync<DeleteExpenseCommand, Deleted>(
            new DeleteExpenseCommand(id), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetCategories(
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<List<ExpenseCategoryResponse>> result = await dispatcher
            .DispatchAsync<GetExpenseCategoriesQuery, List<ExpenseCategoryResponse>>(
                new GetExpenseCategoriesQuery(activeOnly ?? true), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> CreateCategory(
        CreateExpenseCategoryRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<CreateExpenseCategoryCommand, Guid>(
            new CreateExpenseCategoryCommand(request.Name), cancellationToken);

        return ApiResults.Created(result);
    }

    private static async Task<IResult> UpdateCategory(
        Guid id,
        UpdateExpenseCategoryRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Updated> result = await dispatcher.DispatchAsync<UpdateExpenseCategoryCommand, Updated>(
            new UpdateExpenseCategoryCommand(id, request.Name), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> DeleteCategory(
        Guid id,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Deleted> result = await dispatcher.DispatchAsync<DeleteExpenseCategoryCommand, Deleted>(
            new DeleteExpenseCategoryCommand(id), cancellationToken);

        return ApiResults.From(result);
    }
}

public sealed record CreateExpenseRequest(
    DateOnly ExpenseDate,
    Guid? CategoryId,
    string? Description,
    decimal Amount,
    Guid AccountId);

public sealed record UpdateExpenseRequest(
    DateOnly ExpenseDate,
    Guid? CategoryId,
    string? Description,
    decimal Amount,
    Guid AccountId);

public sealed record CreateExpenseCategoryRequest(string Name);

public sealed record UpdateExpenseCategoryRequest(string Name);
