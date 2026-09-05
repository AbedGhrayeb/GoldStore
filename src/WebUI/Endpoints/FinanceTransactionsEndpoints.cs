// <copyright file="FinanceTransactionsEndpoints.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Common.Models;
using Application.Finance.Transactions;
using Application.Finance.Transactions.GetPaged;
using Application.Finance.Transactions.GetRecent;
using Domain.Tenants;
using Microsoft.AspNetCore.Builder;
using SharedKernel.Result;
using WebUI.Extensions;

namespace WebUI.Endpoints;

/// <summary>
/// Tenant-scoped financial transaction query endpoints.
/// </summary>
public sealed class FinanceTransactionsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup($"/{ApiRoutes.Tenant}/finance/transactions")
            .WithTags("Finance Transactions")
            .RequireAuthorization()
            .RequireAuthorization($"feature:{Features.Finance}")
            .RequireAuthorization("finance.view")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/", GetTransactions)
            .WithSummary("Return paged financial transactions.")
            .Produces<PaginatedList<RecentTransactionResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet("/recent", GetRecentTransactions)
            .WithSummary("Return recent financial transactions.")
            .Produces<List<RecentTransactionResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> GetTransactions(
        int? page,
        int? pageSize,
        string? accountName,
        DateTime? fromDate,
        DateTime? toDate,
        string? currency,
        string? accountType,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<PaginatedList<RecentTransactionResponse>> result = await dispatcher
            .DispatchAsync<GetPagedTransactionsQuery, PaginatedList<RecentTransactionResponse>>(
                new GetPagedTransactionsQuery(
                    page ?? 1,
                    pageSize ?? 20,
                    accountName,
                    fromDate,
                    toDate,
                    currency,
                    accountType),
                cancellationToken);

        return ApiResults.From(result);
    }

    private static async Task<IResult> GetRecentTransactions(
        int? count,
        IQueryDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        Result<List<RecentTransactionResponse>> result = await dispatcher
            .DispatchAsync<GetRecentTransactionsQuery, List<RecentTransactionResponse>>(
                new GetRecentTransactionsQuery(count ?? 20), cancellationToken);

        return ApiResults.From(result);
    }
}
