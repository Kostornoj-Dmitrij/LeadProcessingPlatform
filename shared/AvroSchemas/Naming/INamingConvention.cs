namespace AvroSchemas.Naming;

/// <summary>
/// Контракт для формирования имён ресурсов с учётом префиксов/суффиксов
/// </summary>
public interface INamingConvention
{
    string GetTopicName(string baseTopic);

    string GetDatabaseName(string baseDatabase);

    string GetDlqTopicName(string serviceName);
}