namespace MediCore.Abstractions;

/// <summary>Send side of the mediator. Inject this when you only need to dispatch requests.</summary>
public interface ISender
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
