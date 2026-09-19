namespace MediCore.Abstractions;

/// <summary>
/// Strategy that decides HOW the handlers of a notification are invoked
/// (sequentially, in parallel, fire-and-forget, ...).
/// </summary>
public interface INotificationPublisher
{
    Task PublishAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification;
}
