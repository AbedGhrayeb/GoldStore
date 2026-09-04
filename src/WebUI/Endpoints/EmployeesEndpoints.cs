using Application.Abstractions.Messaging;
using Application.Common.Models;
using Application.Employees;
using Application.Employees.Create;
using Application.Employees.GetAll;
using Application.Employees.GetById;
using Application.Employees.GetSalaryPayments;
using Application.Employees.GetSalaryPeriodSummary;
using Application.Employees.GetUnlinkedUsers;
using Application.Employees.PaySalary;
using Application.Employees.ToggleActive;
using Application.Employees.Update;
using Domain.Common;
using Domain.Employees;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using SharedKernel;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Tenant-scoped employee and salary endpoints.
/// </summary>
public sealed class EmployeesEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/employees")
            .WithTags("Employees")
            .RequireAuthorization()
            .RequireAuthorization($"feature:{Features.Hr}")
            .RequireAuthorization(policy => policy.RequireRole("store_admin"))
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/", GetEmployees)
            .WithSummary("List employees.")
            .Produces<List<EmployeeResponse>>(StatusCodes.Status200OK);

        group.MapGet("/salary-payments", GetSalaryPayments)
            .WithSummary("Return paged salary payments.")
            .Produces<PaginatedList<SalaryPaymentResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet("/salary-period-summary", GetSalaryPeriodSummary)
            .WithSummary("Return an employee salary-period summary.")
            .Produces<SalaryPeriodSummaryResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/unlinked-users", GetUnlinkedUsers)
            .WithSummary("List tenant users not linked to an employee.")
            .Produces<List<EmployeeUserOptionResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", GetEmployeeById)
            .WithSummary("Return an employee by ID.")
            .Produces<EmployeeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateEmployee)
            .WithSummary("Create an employee.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateEmployee)
            .WithSummary("Update an employee.")
            .Produces<Updated>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/toggle-active", ToggleActiveEmployee)
            .WithSummary("Toggle an employee's active state.")
            .Produces<Updated>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/pay-salary", PaySalary)
            .WithSummary("Pay an employee salary through a financial account.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> GetEmployees(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<List<EmployeeResponse>> result = await dispatcher.DispatchAsync<GetEmployeesQuery, List<EmployeeResponse>>(
            new GetEmployeesQuery(), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetEmployeeById(
        Guid id,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<EmployeeResponse> result = await dispatcher.DispatchAsync<GetEmployeeByIdQuery, EmployeeResponse>(
            new GetEmployeeByIdQuery(id), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> CreateEmployee(
        CreateEmployeeRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<CreateEmployeeCommand, Guid>(
            new CreateEmployeeCommand(
                request.FirstName,
                request.LastName,
                request.Role,
                request.Salary,
                request.Currency,
                request.SalaryCycle,
                request.ConnectToUser,
                request.ExistingUserId,
                request.NewUserEmail,
                request.NewUserPassword),
            cancellationToken);

        return ApiResults.Created(result);
    }

    private static async Task<IResult> UpdateEmployee(
        Guid id,
        UpdateEmployeeRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Updated> result = await dispatcher.DispatchAsync<UpdateEmployeeCommand, Updated>(
            new UpdateEmployeeCommand(
                id,
                request.FirstName,
                request.LastName,
                request.Role,
                request.Salary,
                request.Currency,
                request.SalaryCycle,
                request.IsActive),
            cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> ToggleActiveEmployee(
        Guid id,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Updated> result = await dispatcher.DispatchAsync<ToggleActiveEmployeeCommand, Updated>(
            new ToggleActiveEmployeeCommand(id), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> PaySalary(
        Guid id,
        PaySalaryRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<PaySalaryCommand, Guid>(
            new PaySalaryCommand(
                id,
                request.AccountId,
                request.Amount,
                request.PaymentDate,
                request.Notes),
            cancellationToken);

        return ApiResults.Created(result);
    }

    private static async Task<IResult> GetSalaryPayments(
        int? page,
        int? pageSize,
        string? employeeName,
        DateTime? fromDate,
        DateTime? toDate,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<PaginatedList<SalaryPaymentResponse>> result = await dispatcher
            .DispatchAsync<GetSalaryPaymentsQuery, PaginatedList<SalaryPaymentResponse>>(
                new GetSalaryPaymentsQuery(
                    page ?? 1,
                    pageSize ?? 20,
                    employeeName,
                    fromDate,
                    toDate),
                cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetSalaryPeriodSummary(
        Guid employeeId,
        DateOnly paymentDate,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<SalaryPeriodSummaryResponse> result = await dispatcher
            .DispatchAsync<GetSalaryPeriodSummaryQuery, SalaryPeriodSummaryResponse>(
                new GetSalaryPeriodSummaryQuery(employeeId, paymentDate), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetUnlinkedUsers(
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<List<EmployeeUserOptionResponse>> result = await dispatcher
            .DispatchAsync<GetUnlinkedUsersQuery, List<EmployeeUserOptionResponse>>(
                new GetUnlinkedUsersQuery(), cancellationToken);

        return ApiResults.From(result);
    }
}

public sealed record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    RoleEnum Role,
    decimal Salary,
    Currency Currency,
    SalaryCycleEnum SalaryCycle,
    bool ConnectToUser,
    Guid? ExistingUserId,
    string? NewUserEmail,
    string? NewUserPassword);

public sealed record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    RoleEnum Role,
    decimal Salary,
    Currency Currency,
    SalaryCycleEnum SalaryCycle,
    bool IsActive);

public sealed record PaySalaryRequest(
    Guid AccountId,
    decimal Amount,
    DateOnly PaymentDate,
    string? Notes);
