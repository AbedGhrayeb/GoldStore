using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Common.Messaging;

/// <summary>
/// Resolves the command handler for the given command from the current scope and
/// delegates to it. Handlers are registered through Scrutor scanning, so the
/// validation and logging decorators run exactly as they do for MVC dispatch.
/// </summary>
internal sealed class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    public Task<Result<TResult>> DispatchAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken)
        where TCommand : ICommand<TResult>
    {
        ICommandHandler<TCommand, TResult> handler = ResolveHandler<TCommand, TResult>();
        return handler.Handle(command, cancellationToken);
    }

    private ICommandHandler<TCommand, TResult> ResolveHandler<TCommand, TResult>()
        where TCommand : ICommand<TResult>
    {
        Type handlerType = typeof(ICommandHandler<,>).MakeGenericType(typeof(TCommand), typeof(TResult));

        object? handler = serviceProvider.GetService(handlerType);
        if (handler is null)
        {
            throw new InvalidOperationException($"No command handler registered for '{typeof(TCommand).Name}'.");
        }

        return (ICommandHandler<TCommand, TResult>)handler;
    }
}
