using SharedKernel.Result;

namespace Application.Abstractions.Messaging;

public interface ICommandDispatcher
{
    Task<Result<TResult>> DispatchAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>;
}
