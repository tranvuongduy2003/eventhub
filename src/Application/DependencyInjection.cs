using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Behaviors;
using EventHub.Application.Options;
using EventHub.Application.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Application;

public static partial class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(AssemblyReference.Assembly));

        services.AddValidatorsFromAssembly(AssemblyReference.Assembly);
        services.AddDomainEventHandlers(AssemblyReference.Assembly);

        services.AddScoped<IPendingDomainEventsCollector, PendingDomainEventsCollector>();
        services.AddScoped<IPendingSessionCacheCollector, PendingSessionCacheCollector>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DomainEventDispatchBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PostCommitSessionCacheBehavior<,>));

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddOptions<ConcurrencyOptions>()
            .BindConfiguration(ConcurrencyOptions.SectionName);

        services.AddOptions<AuthSessionOptions>()
            .BindConfiguration(AuthSessionOptions.SectionName);

        services.AddOptions<StorageOptions>()
            .BindConfiguration(StorageOptions.SectionName);

        services.AddOptions<MessagingOptions>()
            .BindConfiguration(MessagingOptions.SectionName);

        services.AddOptions<RealtimeOptions>()
            .BindConfiguration(RealtimeOptions.SectionName);

        return services;
    }
}
