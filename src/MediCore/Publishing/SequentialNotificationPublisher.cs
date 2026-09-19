using MediCore.Abstractions;

namespace MediCore.Publishing;

/// <summary>Invokes notification handlers one after another. A failing handler stops the rest. (Default)</summary>
public sealed class SequentialNotificationPublisher : INotificationPublisher
{
    public async Task PublishAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        foreach (var handler in handlers)
            await handler.HandleAsync(notification, cancellationToken).ConfigureAwait(false);
    }
}
