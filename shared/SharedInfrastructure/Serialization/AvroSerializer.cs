using Avro.Specific;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Logging;

namespace SharedInfrastructure.Serialization;

/// <summary>
/// Сериализатор Avro сообщений для отправки в Kafka
/// </summary>
public class AvroSerializer<T> : IAsyncSerializer<T> where T : class, ISpecificRecord
{
    private readonly ILogger<AvroSerializer<T>>? _logger;
    private readonly Confluent.SchemaRegistry.Serdes.AvroSerializer<T> _serializer;

    public AvroSerializer(ISchemaRegistryClient schemaRegistry, ILogger<AvroSerializer<T>>? logger = null)
    {
        _logger = logger;

        var config = new AvroSerializerConfig
        {
            AutoRegisterSchemas = false,
            UseLatestVersion = false,
            BufferBytes = 1024
        };

        _serializer = new Confluent.SchemaRegistry.Serdes.AvroSerializer<T>(schemaRegistry, config);
    }

    public async Task<byte[]> SerializeAsync(T data, SerializationContext context)
    {
        try
        {
            var result = await _serializer.SerializeAsync(data, context);
            _logger?.LogDebug("Serialized {Type} to {Length} bytes", typeof(T).Name, result.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to serialize {Type}", typeof(T).Name);
            throw;
        }
    }
}