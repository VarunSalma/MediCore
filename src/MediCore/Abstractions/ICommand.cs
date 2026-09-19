namespace MediCore.Abstractions;

/// <summary>A request that changes state (write side of CQRS). Commands are wrapped in a transaction by default.</summary>
public interface ICommand<out TResponse> : IRequest<TResponse> { }

/// <summary>A state-changing request that returns nothing.</summary>
public interface ICommand : ICommand<Unit> { }
