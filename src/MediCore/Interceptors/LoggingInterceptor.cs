using System.Diagnostics;
using MediCore.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediCore.Interceptors;

/// <summary>Logs the start, duration and failure of every request.</summary>
public sealed class LoggingInterceptor<TRequest, TResponse> : IRequestInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger _logger;

    public LoggingInterceptor(ILogger<LoggingInterceptor<TRequest, TResponse>>? logger = null)
        => _logger = (ILogger?)logger ?? NullLogger.Instance;

    public async Task<TResponse> InterceptAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        _logger.LogInformation("[MediCore] Handling {Request}", name);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);
            _logger.LogInformation("[MediCore] Handled {Request} in {ElapsedMs} ms", name, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MediCore] {Request} failed after {ElapsedMs} ms", name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
