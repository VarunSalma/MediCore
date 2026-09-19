using MediCore.Abstractions;

namespace MediCore.Exceptions;

/// <summary>Thrown by the validation interceptor when one or more validators report errors.</summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IReadOnlyList<ValidationError> errors)
        : base("Validation failed: " + string.Join("; ", errors.Select(e => $"{e.PropertyName}: {e.Message}")))
        => Errors = errors;

    public IReadOnlyList<ValidationError> Errors { get; }
}
