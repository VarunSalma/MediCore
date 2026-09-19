namespace MediCore.Abstractions;

/// <summary>Publish side of the mediator. Inject this when you only need to raise notifications.</summary>
public interface IPublisher
{
    /// <remarks>
    /// Handlers are resolved for the compile-time type <typeparamref name="TNotification"/>,
    /// so publish the concrete notification type, not a variable typed as <see cref="INotification"/>.
    /// </remarks>
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
