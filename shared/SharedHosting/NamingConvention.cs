using AvroSchemas.Naming;
using Microsoft.Extensions.Options;
using SharedHosting.Options;

namespace SharedHosting;

/// <summary>
/// Реализация конвенции именования
/// </summary>
public class NamingConvention(IOptions<NamingOptions> options) : INamingConvention
{
    private readonly NamingOptions _options = options.Value;

    public string GetTopicName(string baseTopic)
    {
        return $"{_options.TopicPrefix}{baseTopic}{_options.TopicSuffix}";
    }

    public string GetDatabaseName(string baseDatabase)
    {
        return $"{_options.DbPrefix}{baseDatabase}{_options.DbSuffix}";
    }

    public string GetDlqTopicName(string serviceName)
    {
        var baseDlq = $"{serviceName}-dlq";
        return GetTopicName(baseDlq);
    }
}