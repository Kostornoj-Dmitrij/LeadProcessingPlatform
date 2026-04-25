using System.Collections.Concurrent;
using AvroSchemas.Messages.Base;

namespace SharedInfrastructure.EventBus;

/// <summary>
/// Кэш для получения LeadId из Avro-событий без рефлексии на каждый вызов
/// </summary>
public static class LeadIdCache
{
    private static readonly ConcurrentDictionary<Type, System.Reflection.PropertyInfo?> Cache = new();

    public static string? GetLeadId(IntegrationEventAvro avroEvent)
    {
        var type = avroEvent.GetType();
        var prop = Cache.GetOrAdd(type, t => t.GetProperty("LeadId"));

        if (prop?.GetValue(avroEvent) is Guid leadId)
            return leadId.ToString();

        return null;
    }
}