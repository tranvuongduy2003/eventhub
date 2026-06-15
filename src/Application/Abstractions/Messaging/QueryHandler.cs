using EventHub.Application.Common;

namespace EventHub.Application.Abstractions.Messaging;

public abstract class QueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public abstract Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
