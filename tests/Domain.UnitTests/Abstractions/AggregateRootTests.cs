using EventHub.Domain.Abstractions;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Abstractions;

public class AggregateRootTests
{
    private sealed record TestEvent : DomainEvent;

    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public TestAggregate()
        {
            Id = Guid.NewGuid();
        }

        public void DoSomething() => Raise(new TestEvent());
    }

    [Fact]
    public void Raise_AddsDomainEvent_ClearRemovesAll()
    {
        var aggregate = new TestAggregate();

        aggregate.DoSomething();
        aggregate.DomainEvents.Should().HaveCount(1);

        aggregate.ClearDomainEvents();
        aggregate.DomainEvents.Should().BeEmpty();
    }
}
