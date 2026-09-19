namespace MediCore.Abstractions;

/// <summary>Represents the next step in the interceptor chain (either another interceptor or the handler).</summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Wraps the execution of a request handler. Use it for cross-cutting concerns such as
/// logging, validation, caching, transactions or retries.
/// Call <c>next</c> to continue the chain; skip it to short-circuit.
/// </summary>
public interface IRequestInterceptor<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> InterceptAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
