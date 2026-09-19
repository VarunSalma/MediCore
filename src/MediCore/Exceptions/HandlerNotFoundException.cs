namespace MediCore.Exceptions;

/// <summary>Thrown when a request is sent but no handler was registered for it.</summary>
public sealed class HandlerNotFoundException : InvalidOperationException
{
    public HandlerNotFoundException(Type requestType)
        : base($"No handler is registered for request '{requestType.FullName}'. " +
               "Make sure the handler's assembly was added with RegisterHandlersFromAssembly(...).")
        => RequestType = requestType;

    public Type RequestType { get; }
}
