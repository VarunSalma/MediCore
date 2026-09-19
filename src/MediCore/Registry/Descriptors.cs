namespace MediCore.Registry;

/// <summary>Describes one discovered request handler.</summary>
public sealed record RequestHandlerDescriptor(
    Type RequestType,
    Type ResponseType,
    Type ServiceType,
    Type ImplementationType);

/// <summary>Describes one discovered notification handler.</summary>
public sealed record NotificationHandlerDescriptor(
    Type NotificationType,
    Type ServiceType,
    Type ImplementationType);

/// <summary>Describes one interceptor and its position in the chain (0 = outermost).</summary>
public sealed record InterceptorDescriptor(Type ImplementationType, int Order);
