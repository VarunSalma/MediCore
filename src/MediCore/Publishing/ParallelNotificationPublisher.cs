using MediCore.Abstractions;

namespace MediCore.Publishing;

/// <summary>Starts all notification handlers at once and waits for all of them.</summary>
public sealed class ParallelNotificationPublisher : INotificationPublisher
{
    public Task PublishAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var tasks = handlers.Select(h => h.HandleAsync(notification, cancellationToken)).ToArray();
        return Task.WhenAll(tasks);
    }
}
