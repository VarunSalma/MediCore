using System.Diagnostics.CodeAnalysis;

namespace MediCore.Registry;

/// <summary>Default <see cref="IServiceRegistry"/>. Mutated only during startup by the scanner.</summary>
public sealed class ServiceRegistry : IServiceRegistry
{
    private readonly Dictionary<Type, RequestHandlerDescriptor> _requestHandlers = new();
    private readonly List<NotificationHandlerDescriptor> _notificationHandlers = new();
    private readonly List<InterceptorDescriptor> _interceptors = new();

    public IReadOnlyCollection<RequestHandlerDescriptor> RequestHandlers => _requestHandlers.Values;
    public IReadOnlyCollection<NotificationHandlerDescriptor> NotificationHandlers => _notificationHandlers;
    public IReadOnlyList<InterceptorDescriptor> Interceptors => _interceptors;

    public bool TryGetRequestHandler(Type requestType, [NotNullWhen(true)] out RequestHandlerDescriptor? descriptor)
        => _requestHandlers.TryGetValue(requestType, out descriptor);

    public IEnumerable<NotificationHandlerDescriptor> GetNotificationHandlers(Type notificationType)
        => _notificationHandlers.Where(h => h.NotificationType == notificationType);

    internal void AddRequestHandler(RequestHandlerDescriptor descriptor)
    {
        if (_requestHandlers.TryGetValue(descriptor.RequestType, out var existing))
        {
            if (existing.ImplementationType == descriptor.ImplementationType) return;

            throw new InvalidOperationException(
                $"Request '{descriptor.RequestType.Name}' has more than one handler: " +
                $"'{existing.ImplementationType.Name}' and '{descriptor.ImplementationType.Name}'. " +
                "A request must have exactly one handler.");
        }
        _requestHandlers.Add(descriptor.RequestType, descriptor);
    }

    internal void AddNotificationHandler(NotificationHandlerDescriptor descriptor)
    {
        if (_notificationHandlers.Any(h => h.NotificationType == descriptor.NotificationType &&
                                           h.ImplementationType == descriptor.ImplementationType))
            return;

        _notificationHandlers.Add(descriptor);
    }

    internal void AddInterceptor(Type implementationType)
        => _interceptors.Add(new InterceptorDescriptor(implementationType, _interceptors.Count));
}
