using System.Text.Json;

namespace LoadTests.Host.Infrastructure;

/// <summary>
/// Вспомогательный класс для получения и кэширования JWT токенов
/// </summary>
public static class AuthHelper
{
    private static readonly HttpClient HttpClient = new();
    private static string? _cachedToken;
    private static DateTime _tokenExpiry = DateTime.MinValue;

    public static async Task<string> GetTokenAsync(string apiGatewayUrl)
    {
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
            return _cachedToken;

        var tokenRequest = new { clientId = "partner-system", apiKey = "dev-api-key" };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{apiGatewayUrl}/api/auth/token");
        request.Content = tokenRequest.ToJsonContent();

        var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        _cachedToken = json.RootElement.GetProperty("token").GetString();
        _tokenExpiry = DateTime.UtcNow.AddSeconds(json.RootElement.GetProperty("expiresIn").GetInt32());

        return _cachedToken!;
    }
}