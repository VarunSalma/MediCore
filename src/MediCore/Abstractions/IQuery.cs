namespace MediCore.Abstractions;

/// <summary>A request that only reads state (read side of CQRS). Queries are never wrapped in a transaction.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse> { }
