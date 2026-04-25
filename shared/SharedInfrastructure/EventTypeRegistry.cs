using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using AvroSchemas.Messages.Base;
using SharedInfrastructure.EventBus;

namespace SharedInfrastructure;

/// <summary>
/// Кэш типов интеграционных событий
/// </summary>
public static class EventTypeRegistry
{
    private static readonly ConcurrentDictionary<string, Type> TypeCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<Type, Delegate> PublisherCache = new();
    private static bool _initialized;

    private static readonly MethodInfo PublishAsyncMethod = typeof(IEventBus)
        .GetMethods()
        .First(m => m.Name == nameof(IEventBus.PublishAsync) && m.IsGenericMethod);

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

    public static Func<IEventBus, object, CancellationToken, Task> GetPublisher(Type eventType)
    {
        return (Func<IEventBus, object, CancellationToken, Task>)PublisherCache.GetOrAdd(eventType, type =>
        {
            var eventBusParam = Expression.Parameter(typeof(IEventBus), "eventBus");
            var eventParam = Expression.Parameter(typeof(object), "evt");
            var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

            var castedEvent = Expression.Convert(eventParam, type);

            var closedMethod = PublishAsyncMethod.MakeGenericMethod(type);

            var call = Expression.Call(eventBusParam, closedMethod, castedEvent, ctParam);

            var lambda = Expression.Lambda<Func<IEventBus, object, CancellationToken, Task>>(
                call, eventBusParam, eventParam, ctParam);

            return lambda.Compile();
        });
    }
}