using EventHub.Application.Common;
using MediatR;

namespace EventHub.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
