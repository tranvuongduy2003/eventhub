namespace Solution.Domain.Abstractions;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
