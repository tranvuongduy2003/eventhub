namespace EventHub.Domain.Abstractions;

public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        OccurredOn = DateTimeOffset.UtcNow;
    }

    protected DomainEvent(DateTimeOffset occurredOn)
    {
        OccurredOn = occurredOn;
    }

    public DateTimeOffset OccurredOn { get; init; }
}
