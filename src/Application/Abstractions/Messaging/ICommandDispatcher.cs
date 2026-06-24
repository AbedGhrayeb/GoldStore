using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions.Messaging;

public interface ICommandDispatcher
{
    Task<TResult> DispatchAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default);
}
