using EventHub.Application.Abstractions.Email;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Payments;
using EventHub.Application.Abstractions.Storage;
using FluentAssertions;

namespace EventHub.Domain.UnitTests;

public class EventHubSkeletonTests
{
    [Theory]
    [InlineData(typeof(EventHub.Domain.Events.ModuleMarker))]
    [InlineData(typeof(EventHub.Domain.Orders.ModuleMarker))]
    [InlineData(typeof(EventHub.Domain.Payments.ModuleMarker))]
    [InlineData(typeof(EventHub.Domain.Tickets.ModuleMarker))]
    public void BoundedContextModules_ArePresent(Type moduleMarkerType)
    {
        moduleMarkerType.Assembly.Should().BeSameAs(EventHub.Domain.AssemblyReference.Assembly);
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
