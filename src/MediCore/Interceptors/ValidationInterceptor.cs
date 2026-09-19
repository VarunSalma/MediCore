using MediCore.Abstractions;
using MediCore.Exceptions;

namespace MediCore.Interceptors;

/// <summary>
/// Runs every <see cref="IValidator{T}"/> registered for the request and throws
/// <see cref="ValidationException"/> before the handler executes if any of them fails.
/// </summary>
public sealed class ValidationInterceptor<TRequest, TResponse> : IRequestInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationInterceptor(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> InterceptAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
            errors.AddRange(result.Errors);
        }

        if (errors.Count > 0)
            throw new ValidationException(errors);

        return await next().ConfigureAwait(false);
    }
}
