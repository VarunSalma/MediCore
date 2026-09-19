namespace MediCore.Abstractions;

/// <summary>Marker for every message that is sent to exactly one handler.</summary>
public interface IBaseRequest { }

/// <summary>A request that produces <typeparamref name="TResponse"/>.</summary>
public interface IRequest<out TResponse> : IBaseRequest { }

/// <summary>A request that produces no meaningful value.</summary>
public interface IRequest : IRequest<Unit> { }
