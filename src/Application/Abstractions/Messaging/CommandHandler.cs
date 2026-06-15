using EventHub.Application.Common;

namespace EventHub.Application.Abstractions.Messaging;

public abstract class CommandHandler<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public abstract Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}
public abstract class CommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public abstract Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
