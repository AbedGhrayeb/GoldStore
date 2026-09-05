// <copyright file="QueryDispatcher.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Common.Messaging;

/// <summary>
/// Resolves the query handler for the given query from the current scope and
/// delegates to it. Handlers are registered through Scrutor scanning, so the
/// logging decorator runs exactly as it does for MVC dispatch.
/// </summary>
internal sealed class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    public Task<Result<TResult>> DispatchAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken)
        where TQuery : IQuery<TResult>
    {
        IQueryHandler<TQuery, TResult> handler = this.ResolveHandler<TQuery, TResult>();
        return handler.Handle(query, cancellationToken);
    }

    private IQueryHandler<TQuery, TResult> ResolveHandler<TQuery, TResult>()
        where TQuery : IQuery<TResult>
    {
        Type handlerType = typeof(IQueryHandler<,>).MakeGenericType(typeof(TQuery), typeof(TResult));

        object? handler = serviceProvider.GetService(handlerType) ?? throw new InvalidOperationException($"No query handler registered for '{typeof(TQuery).Name}'.");

        return (IQueryHandler<TQuery, TResult>)handler;
    }
}
