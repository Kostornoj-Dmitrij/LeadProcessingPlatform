namespace SharedHosting.Constants;

/// <summary>
/// Ключи конфигурации
/// </summary>
public static class ConfigurationKeys
{
    public const string DefaultConnection = "DefaultConnection";
    public const string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";
    
    public const string KafkaBootstrapServers = "Kafka:BootstrapServers";
    public const string KafkaSchemaRegistryUrl = "Kafka:SchemaRegistryUrl";
    public const string KafkaGroupId = "Kafka:GroupId";
    public const string KafkaDlqTopic = "Kafka:DlqTopic";
    
    public const string OpenTelemetrySection = "OpenTelemetry";
    public const string OtlpGrpcPort = "4317";
    
    public const string PostgresUniqueViolationSqlState = "23505";
    
    public const string HealthPath = "/health";
    public const string SwaggerPath = "/swagger";
    public const string SwaggerJsonPath = "/swagger/v1/swagger.json";
    public const string SubjectsPath = "/subjects";
    public const string SchemasPath = "/schemas";
}