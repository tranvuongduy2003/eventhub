using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Solution.Application.Abstractions.Auth;
using Solution.Application.Abstractions.Services;
using Solution.Application.Behaviors;
using Solution.Application.Options;
using Solution.Application.Services;

namespace Solution.Application;

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

        return services;
    }
}
