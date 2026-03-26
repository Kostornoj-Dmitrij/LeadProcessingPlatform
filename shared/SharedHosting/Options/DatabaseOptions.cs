namespace SharedHosting.Options;

/// <summary>
/// Настройки подключения к базе данных
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    public string DefaultConnection { get; set; } = string.Empty;

    public int MaxRetryCount { get; set; } = 5;

    public int CommandTimeout { get; set; } = 30;

    public bool EnableSensitiveDataLogging { get; set; } = false;
}