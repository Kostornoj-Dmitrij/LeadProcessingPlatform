namespace ApiGateway.Host.Models;

/// <summary>
/// Запрос на получение JWT токена
/// </summary>
public class TokenRequest
{
    public string ClientId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}