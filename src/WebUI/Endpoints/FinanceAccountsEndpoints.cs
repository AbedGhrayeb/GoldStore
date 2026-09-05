// <copyright file="FinanceAccountsEndpoints.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Finance.Accounts;
using Application.Finance.Accounts.Create;
using Application.Finance.Accounts.GetAll;
using Application.Finance.Accounts.GetBalance;
using Application.Finance.Accounts.GetWithBalance;
using Application.Finance.Accounts.SetBalance;
using Domain.Common;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using SharedKernel;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Tenant-scoped financial account and balance endpoints.
/// </summary>
public sealed class FinanceAccountsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/finance/accounts")
            .WithTags("Finance Accounts")
            .RequireAuthorization()
            .RequireAuthorization($"feature:{Features.Finance}")
            .RequireAuthorization("finance.view")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/", GetAccounts)
            .WithSummary("List financial accounts.")
            .Produces<List<FinancialAccountResponse>>(StatusCodes.Status200OK);

        group.MapGet("/with-balances", GetAccountsWithBalances)
            .WithSummary("List financial accounts with their derived balances.")
            .Produces<List<AccountWithBalanceResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}/balance", GetAccountBalance)
            .WithSummary("Return the derived balance of a financial account.")
            .Produces<AccountBalanceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAccount)
                    .RequireAuthorization("finance.manage")
            .WithSummary("Create a financial account.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/balance", SetAccountBalance)
                    .RequireAuthorization("finance.manage")
            .WithSummary("Set a financial account balance through a ledger adjustment.")
            .Produces<Updated>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> GetAccounts(
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<List<FinancialAccountResponse>> result = await dispatcher
            .DispatchAsync<GetFinancialAccountsQuery, List<FinancialAccountResponse>>(
                new GetFinancialAccountsQuery(activeOnly ?? true), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetAccountsWithBalances(
        string? accountType,
        bool? activeOnly,
        Currency? currency,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<List<AccountWithBalanceResponse>> result = await dispatcher
            .DispatchAsync<GetAccountsWithBalancesQuery, List<AccountWithBalanceResponse>>(
                new GetAccountsWithBalancesQuery(accountType, activeOnly ?? true, currency),
                cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetAccountBalance(
        Guid id,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<AccountBalanceResponse> result = await dispatcher
            .DispatchAsync<GetAccountBalanceQuery, AccountBalanceResponse>(
                new GetAccountBalanceQuery(id), cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> CreateAccount(
        CreateFinancialAccountRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await dispatcher.DispatchAsync<CreateFinancialAccountCommand, Guid>(
            new CreateFinancialAccountCommand(
                request.Name,
                request.Currency,
                request.AccountNumber,
                request.Notes,
                request.OpeningBalance),
            cancellationToken);

        return ApiResults.Created(result);
    }

    private static async Task<IResult> SetAccountBalance(
        Guid id,
        SetAccountBalanceRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<Updated> result = await dispatcher.DispatchAsync<SetAccountBalanceCommand, Updated>(
            new SetAccountBalanceCommand(id, request.TargetBalance, request.Notes),
            cancellationToken);

        return ApiResults.From(result);
    }
}

public sealed record CreateFinancialAccountRequest(
    string Name,
    string Currency,
    string? AccountNumber,
    string? Notes,
    decimal OpeningBalance = 0m);

public sealed record SetAccountBalanceRequest(decimal TargetBalance, string? Notes);
