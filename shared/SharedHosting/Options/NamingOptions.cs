namespace SharedHosting.Options;

/// <summary>
/// Настройки именования ресурсов
/// </summary>
public class NamingOptions
{
    public const string SectionName = "Naming";

    public string TopicPrefix { get; set; } = string.Empty;

    public string TopicSuffix { get; set; } = string.Empty;

    public string DbPrefix { get; set; } = string.Empty;

    public string DbSuffix { get; set; } = string.Empty;
}