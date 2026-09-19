using System.Reflection;
using MediCore.Abstractions;
using MediCore.Interceptors;
using MediCore.Publishing;
using MediCore.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Registry;

/// <summary>Fluent configuration (Builder + Options pattern) used by <c>services.AddMediCore(...)</c>.</summary>
public sealed class MediCoreOptions
{
    internal List<Assembly> Assemblies { get; } = new();
    internal List<Type> HandlerTypes { get; } = new();
    internal List<Type> InterceptorTypes { get; } = new();
    internal Type NotificationPublisherType { get; private set; } = typeof(SequentialNotificationPublisher);
    internal Type TransactionManagerType { get; private set; } = typeof(TransactionScopeManager);

    /// <summary>Lifetime of handlers and interceptors. Default: Transient.</summary>
    public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>Lifetime of the <see cref="ITransactionManager"/>. Default: Scoped.</summary>
    public ServiceLifetime TransactionManagerLifetime { get; set; } = ServiceLifetime.Scoped;

    // ---- handler discovery ----

    public MediCoreOptions RegisterHandlersFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        Assemblies.Add(assembly);
        return this;
    }

    public MediCoreOptions RegisterHandlersFromAssemblyContaining<T>()
        => RegisterHandlersFromAssembly(typeof(T).Assembly);

    /// <summary>Registers specific handler classes instead of scanning a whole assembly.</summary>
    public MediCoreOptions RegisterHandlers(params Type[] handlerTypes)
    {
        HandlerTypes.AddRange(handlerTypes);
        return this;
    }

    // ---- interceptors (order of calls = order of execution, first call is outermost) ----

    /// <summary>
    /// Adds an interceptor. Pass an open generic (<c>typeof(MyInterceptor&lt;,&gt;)</c>) to apply it to every request,
    /// or a closed type to apply it to one specific request.
    /// </summary>
    public MediCoreOptions AddInterceptor(Type interceptorType)
    {
        ArgumentNullException.ThrowIfNull(interceptorType);

        var implementsContract = interceptorType.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestInterceptor<,>));

        if (!implementsContract)
            throw new ArgumentException(
                $"'{interceptorType.Name}' must implement IRequestInterceptor<TRequest, TResponse>.", nameof(interceptorType));

        if (interceptorType.IsGenericTypeDefinition && interceptorType.GetGenericArguments().Length != 2)
            throw new ArgumentException(
                "An open generic interceptor must have exactly two type parameters <TRequest, TResponse>.", nameof(interceptorType));

        InterceptorTypes.Add(interceptorType);
        return this;
    }

    /// <summary>Logging (outermost) → Validation → Transaction (innermost).</summary>
    public MediCoreOptions AddDefaultInterceptors()
        => AddInterceptor(typeof(LoggingInterceptor<,>))
          .AddInterceptor(typeof(ValidationInterceptor<,>))
          .AddInterceptor(typeof(TransactionInterceptor<,>));

    // ---- strategies ----

    public MediCoreOptions UseNotificationPublisher<TPublisher>() where TPublisher : INotificationPublisher
    {
        NotificationPublisherType = typeof(TPublisher);
        return this;
    }

    public MediCoreOptions UseParallelNotificationPublisher()
        => UseNotificationPublisher<ParallelNotificationPublisher>();

    public MediCoreOptions UseTransactionManager<TManager>() where TManager : ITransactionManager
    {
        TransactionManagerType = typeof(TManager);
        return this;
    }
}
