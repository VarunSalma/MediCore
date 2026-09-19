using MediCore.Abstractions;
using MediCore.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Core;

/// <summary>
/// Non-generic-in-request adapter so the mediator can call a strongly typed pipeline
/// knowing only the response type (Adapter + Template Method).
/// </summary>
internal abstract class RequestDispatcher<TResponse>
{
    public abstract Task<TResponse> DispatchAsync(
        IRequest<TResponse> request,
        IServiceProvider services,
        CancellationToken cancellationToken);
}

internal sealed class RequestDispatcher<TRequest, TResponse> : RequestDispatcher<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> DispatchAsync(
        IRequest<TResponse> request,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var handler = services.GetService<IRequestHandler<TRequest, TResponse>>()
                      ?? throw new HandlerNotFoundException(typeof(TRequest));

        var interceptors = services.GetServices<IRequestInterceptor<TRequest, TResponse>>();

        var pipeline = InterceptorPipeline.Build((TRequest)request, handler, interceptors, cancellationToken);
        return pipeline();
    }
}
