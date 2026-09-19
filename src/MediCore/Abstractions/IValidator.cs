namespace MediCore.Abstractions;

public interface IValidator<in T>
{
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken);
}

public sealed record ValidationError(string PropertyName, string Message);

public sealed class ValidationResult
{
    private static readonly ValidationResult SuccessInstance = new(Array.Empty<ValidationError>());

    public ValidationResult(IEnumerable<ValidationError> errors) => Errors = errors.ToList();

    public IReadOnlyList<ValidationError> Errors { get; }
    public bool IsValid => Errors.Count == 0;

    public static ValidationResult Success => SuccessInstance;
    public static ValidationResult Failure(params ValidationError[] errors) => new(errors);
}
