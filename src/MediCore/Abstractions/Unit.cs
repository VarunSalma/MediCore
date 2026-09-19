namespace MediCore.Abstractions;

/// <summary>Represents "no value" for requests that do not return anything.</summary>
public readonly record struct Unit
{
    public static Unit Value => default;
    public static Task<Unit> Task { get; } = System.Threading.Tasks.Task.FromResult(default(Unit));
}
