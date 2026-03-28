using Avro.Specific;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Logging;

namespace SharedInfrastructure.Serialization;

/// <summary>
/// Десериализатор Avro сообщений из Kafka
/// </summary>
public class AvroDeserializer<T> : IAsyncDeserializer<T> where T : class, ISpecificRecord
{
    private readonly ILogger<AvroDeserializer<T>>? _logger;
    private readonly Confluent.SchemaRegistry.Serdes.AvroDeserializer<T> _deserializer;

    public AvroDeserializer(ISchemaRegistryClient schemaRegistry, ILogger<AvroDeserializer<T>>? logger = null)
    {
        _logger = logger;

        var config = new AvroDeserializerConfig
        {
            UseLatestVersion = false
        };

        _deserializer = new Confluent.SchemaRegistry.Serdes.AvroDeserializer<T>(schemaRegistry, config);
    }

    public async Task<T> DeserializeAsync(ReadOnlyMemory<byte> data, bool isNull, SerializationContext context)
    {
        try
        {
            var result = await _deserializer.DeserializeAsync(data, isNull, context);
            _logger?.LogDebug("Deserialized {Type} from {Length} bytes", typeof(T).Name, data.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to deserialize {Type}", typeof(T).Name);
            throw;
        }
    }
}