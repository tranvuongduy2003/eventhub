using MediatR;
using Solution.Application.Common;

namespace Solution.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
