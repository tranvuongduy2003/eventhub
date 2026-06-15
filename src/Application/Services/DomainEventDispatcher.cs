using System.Collections;
using System.Reflection;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Services;
using EventHub.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Application.Services;

internal sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly MethodInfo GetServicesOpenGeneric = typeof(ServiceProviderServiceExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(ServiceProviderServiceExtensions.GetServices) && m.IsGenericMethodDefinition);

    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var getServices = GetServicesOpenGeneric.MakeGenericMethod(handlerType);
            var handlers = (IEnumerable)getServices.Invoke(null, [serviceProvider])!;

            var handleMethod = handlerType.GetMethod(
                "Handle",
                BindingFlags.Public | BindingFlags.Instance,
                [eventType, typeof(CancellationToken)])!;

            foreach (var handler in handlers)
            {
                var task = (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
                await task.ConfigureAwait(false);
            }
        }
    }
}
