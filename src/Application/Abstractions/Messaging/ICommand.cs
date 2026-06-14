using MediatR;
using Solution.Application.Common;

namespace Solution.Application.Abstractions.Messaging;

public interface ICommand : IRequest<Result>, IUnitOfWorkRequest;
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IUnitOfWorkRequest;
