namespace SharedHosting.Options;

/// <summary>
/// Общие настройки хостинга
/// </summary>
public class HostingOptions
{
    public string ServiceName { get; set; } = string.Empty;

    public string Environment { get; set; } = "Development";

    public bool EnableSwagger { get; set; } = true;

    public bool EnableHealthChecks { get; set; } = true;
}