using System.Collections.Concurrent;
using AvroSchemas.Messages.Base;

namespace SharedInfrastructure;

/// <summary>
/// Кэш типов интеграционных событий
/// </summary>
public static class EventTypeRegistry
{
    private static readonly ConcurrentDictionary<string, Type> TypeCache = new(StringComparer.Ordinal);
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;

        lock (TypeCache)
        {
            if (_initialized)
                return;

            var assembly = typeof(IntegrationEventAvro).Assembly;
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract &&
                            typeof(IntegrationEventAvro).IsAssignableFrom(t));

            foreach (var type in types)
            {
                if (type.FullName != null)
                    TypeCache[type.FullName] = type;

                var assemblyQualifiedName = type.AssemblyQualifiedName;
                if (assemblyQualifiedName != null)
                    TypeCache[assemblyQualifiedName] = type;
            }

            _initialized = true;
        }
    }

    public static Type? GetType(string typeName)
    {
        TypeCache.TryGetValue(typeName, out var type);
        return type;
    }

    public static IReadOnlyCollection<Type> AllTypes => TypeCache.Values.Distinct().ToList().AsReadOnly();
}