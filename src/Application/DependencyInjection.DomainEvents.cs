using System.Reflection;
using EventHub.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Application;

public static partial class DependencyInjection
{
    private static IServiceCollection AddDomainEventHandlers(
        this IServiceCollection services,
        Assembly assembly)
    {
        var handlerPairs = assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>))
                .Select(i => new { Service = i, Implementation = t }));

        foreach (var pair in handlerPairs)
        {
            services.AddTransient(pair.Service, pair.Implementation);
        }

        return services;
    }
}
