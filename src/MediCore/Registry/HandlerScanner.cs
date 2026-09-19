using System.Reflection;
using MediCore.Abstractions;

namespace MediCore.Registry;

/// <summary>Finds request and notification handlers by reflection and records them in a <see cref="ServiceRegistry"/>.</summary>
internal static class HandlerScanner
{
    public static void Scan(
        IEnumerable<Assembly> assemblies,
        IEnumerable<Type> explicitTypes,
        ServiceRegistry registry)
    {
        var types = assemblies.SelectMany(GetLoadableTypes)
                              .Concat(explicitTypes)
                              .Distinct();

        foreach (var type in types)
            Register(type, registry);
    }

    private static void Register(Type type, ServiceRegistry registry)
    {
        if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
            return;

        foreach (var contract in type.GetInterfaces().Where(i => i.IsGenericType))
        {
            var definition = contract.GetGenericTypeDefinition();
            var args = contract.GetGenericArguments();

            if (definition == typeof(IRequestHandler<,>))
                registry.AddRequestHandler(new RequestHandlerDescriptor(args[0], args[1], contract, type));
            else if (definition == typeof(INotificationHandler<>))
                registry.AddNotificationHandler(new NotificationHandlerDescriptor(args[0], contract, type));
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null).Select(t => t!);
        }
    }
}
