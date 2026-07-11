using EventHub.Application.Abstractions.Email;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Payments;
using EventHub.Application.Abstractions.Storage;
using EventHub.Domain.DiscountCodes;
using EventHub.Domain.Events;
using EventHub.Domain.Orders;
using EventHub.Domain.Payments;
using EventHub.Domain.Tickets;
using FluentAssertions;

namespace EventHub.Domain.UnitTests;

public class EventHubSkeletonTests
{
    [Theory]
    [InlineData(typeof(Event))]
    [InlineData(typeof(Order))]
    [InlineData(typeof(Payment))]
    [InlineData(typeof(Ticket))]
    [InlineData(typeof(DiscountCode))]
    public void BoundedContextAggregates_ArePresent(Type aggregateType)
    {
        aggregateType.Assembly.Should().BeSameAs(EventHub.Domain.AssemblyReference.Assembly);
    }

    [Fact]
    public void ApplicationPorts_AreDefinedForInfrastructureAdapters()
    {
        typeof(IObjectStorage).Assembly.Should().BeSameAs(EventHub.Application.AssemblyReference.Assembly);
        typeof(IIntegrationEventPublisher).Assembly.Should().BeSameAs(EventHub.Application.AssemblyReference.Assembly);
        typeof(IEmailSender).Assembly.Should().BeSameAs(EventHub.Application.AssemblyReference.Assembly);
        typeof(IPaymentGateway).Assembly.Should().BeSameAs(EventHub.Application.AssemblyReference.Assembly);
    }
}
