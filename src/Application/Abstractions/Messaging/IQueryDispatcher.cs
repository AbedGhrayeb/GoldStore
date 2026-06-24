using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions.Messaging;

public interface IQueryDispatcher
{
    Task<TResult> DispatchAsync<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default);
}
