using EventHub.Domain.Abstractions;

namespace EventHub.Application.Abstractions.Persistence;

public interface IRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TId>
    where TId : notnull;
