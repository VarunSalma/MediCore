using System.Collections.Concurrent;
using MediCore.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Core;

/// <summary>
/// Facade over request dispatching and notification publishing.
/// Consumers should depend on <see cref="ISender"/> / <see cref="IPublisher"/> rather than this class.
/// </summary>
public sealed class Mediator : IMediator
{
    // Flyweight cache: one dispatcher instance per (request type, response type).
    private static readonly ConcurrentDictionary<(Type Request, Type Response), object> Dispatchers = new();

    private readonly IServiceProvider _services;
    private readonly INotificationPublisher _notificationPublisher;

    public Mediator(IServiceProvider services, INotificationPublisher notificationPublisher)
    {
        _services = services;
        _notificationPublisher = notificationPublisher;
    }

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispatcher = (RequestDispatcher<TResponse>)Dispatchers.GetOrAdd(
            (request.GetType(), typeof(TResponse)),
            static key => Activator.CreateInstance(
                typeof(RequestDispatcher<,>).MakeGenericType(key.Request, key.Response))!);

        return dispatcher.DispatchAsync(request, _services, cancellationToken);
    }

    public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var handlers = _services.GetServices<INotificationHandler<TNotification>>();
        return _notificationPublisher.PublishAsync(handlers, notification, cancellationToken);
    }
}
