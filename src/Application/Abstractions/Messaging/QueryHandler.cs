using Solution.Application.Common;

namespace Solution.Application.Abstractions.Messaging;

public abstract class QueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public abstract Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
