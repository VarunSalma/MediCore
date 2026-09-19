using System.Diagnostics.CodeAnalysis;

namespace MediCore.Registry;

/// <summary>
/// Read-only catalogue of everything MediCore discovered at startup.
/// Inject it for diagnostics, health endpoints, docs generation or startup assertions.
/// </summary>
public interface IServiceRegistry
{
    IReadOnlyCollection<RequestHandlerDescriptor> RequestHandlers { get; }
    IReadOnlyCollection<NotificationHandlerDescriptor> NotificationHandlers { get; }

    /// <summary>Interceptors in execution order (outermost first).</summary>
    IReadOnlyList<InterceptorDescriptor> Interceptors { get; }

    bool TryGetRequestHandler(Type requestType, [NotNullWhen(true)] out RequestHandlerDescriptor? descriptor);
    IEnumerable<NotificationHandlerDescriptor> GetNotificationHandlers(Type notificationType);
}
