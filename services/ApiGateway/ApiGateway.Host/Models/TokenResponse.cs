namespace ApiGateway.Host.Models;

/// <summary>
/// Ответ с JWT токеном
/// </summary>
public class TokenResponse
{
    public string Token { get; set; } = string.Empty;

    public int ExpiresIn { get; set; }

    public string TokenType { get; set; } = "Bearer";
}