using MediCore.Abstractions;

namespace MediCore.Core;

/// <summary>
/// Builds the Chain of Responsibility: the first registered interceptor is the outermost one,
/// the request handler is the innermost link.
/// </summary>
internal static class InterceptorPipeline
{
    public static RequestHandlerDelegate<TResponse> Build<TRequest, TResponse>(
        TRequest request,
        IRequestHandler<TRequest, TResponse> handler,
        IEnumerable<IRequestInterceptor<TRequest, TResponse>> interceptors,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        RequestHandlerDelegate<TResponse> pipeline = () => handler.HandleAsync(request, cancellationToken);

        foreach (var interceptor in interceptors.Reverse())
        {
            var next = pipeline;
            var current = interceptor;
            pipeline = () => current.InterceptAsync(request, next, cancellationToken);
        }

        return pipeline;
    }
}
