using Solution.Domain.Abstractions;

namespace Solution.Application.Abstractions.Persistence;

public interface IRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TId>
    where TId : notnull;
