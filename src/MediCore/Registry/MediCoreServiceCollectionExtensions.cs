using MediCore.Abstractions;
using MediCore.Core;
using MediCore.Registry;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class MediCoreServiceCollectionExtensions
{
    /// <summary>
    /// Scans for handlers, fills the <see cref="IServiceRegistry"/> and registers everything in the container.
    /// </summary>
    public static IServiceCollection AddMediCore(this IServiceCollection services, Action<MediCoreOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MediCoreOptions();
        configure(options);

        if (options.Assemblies.Count == 0 && options.HandlerTypes.Count == 0)
            throw new InvalidOperationException(
                "MediCore has nothing to register. Call RegisterHandlersFromAssembly(...) or RegisterHandlers(...).");

        // 1. Discover → registry (single source of truth)
        var registry = new ServiceRegistry();
        HandlerScanner.Scan(options.Assemblies, options.HandlerTypes, registry);
        foreach (var interceptor in options.InterceptorTypes)
            registry.AddInterceptor(interceptor);

        services.AddSingleton<IServiceRegistry>(registry);

        // 2. Registry → container
        foreach (var handler in registry.RequestHandlers)
            services.Add(new ServiceDescriptor(handler.ServiceType, handler.ImplementationType, options.HandlerLifetime));

        foreach (var handler in registry.NotificationHandlers)
            services.Add(new ServiceDescriptor(handler.ServiceType, handler.ImplementationType, options.HandlerLifetime));

        foreach (var interceptor in registry.Interceptors)
            RegisterInterceptor(services, interceptor.ImplementationType, options.HandlerLifetime);

        // 3. Infrastructure (TryAdd → the application can pre-register its own replacements)
        services.TryAdd(new ServiceDescriptor(
            typeof(INotificationPublisher), options.NotificationPublisherType, ServiceLifetime.Singleton));
        services.TryAdd(new ServiceDescriptor(
            typeof(ITransactionManager), options.TransactionManagerType, options.TransactionManagerLifetime));

        services.TryAddTransient<IMediator, Mediator>();
        services.TryAddTransient<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.TryAddTransient<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        return services;
    }

    private static void RegisterInterceptor(IServiceCollection services, Type type, ServiceLifetime lifetime)
    {
        if (type.IsGenericTypeDefinition)
        {
            services.Add(new ServiceDescriptor(typeof(IRequestInterceptor<,>), type, lifetime));
            return;
        }

        foreach (var contract in type.GetInterfaces()
                     .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestInterceptor<,>)))
        {
            services.Add(new ServiceDescriptor(contract, type, lifetime));
        }
    }
}
