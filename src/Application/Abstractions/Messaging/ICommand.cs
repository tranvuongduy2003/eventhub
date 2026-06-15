using EventHub.Application.Common;
using MediatR;

namespace EventHub.Application.Abstractions.Messaging;

public interface ICommand : IRequest<Result>, IUnitOfWorkRequest;
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IUnitOfWorkRequest;
