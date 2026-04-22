using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiGateway.Host.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Host.Controllers;

/// <summary>
/// Контроллер для аутентификации и выдачи JWT токенов
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    : ControllerBase
{
    [HttpPost("token")]
    public IActionResult GetToken([FromBody] TokenRequest request)
    {
        try
        {
            var apiKeys = configuration.GetSection("Auth:ApiKeys").Get<Dictionary<string, string>>();

            if (apiKeys == null || !apiKeys.TryGetValue(request.ClientId, out var expectedKey) || 
                expectedKey != request.ApiKey)
            {
                logger.LogWarning("Invalid authentication attempt for client: {ClientId}", request.ClientId);
                return Unauthorized(new { error = "Invalid client credentials" });
            }

            var token = GenerateJwtToken(request.ClientId);

            logger.LogInformation("Token issued for client: {ClientId}", request.ClientId);

            return Ok(new TokenResponse
            {
                Token = token,
                ExpiresIn = 3600,
                TokenType = "Bearer"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating token for client: {ClientId}", request.ClientId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private string GenerateJwtToken(string clientId)
    {
        var secretKey = configuration["Jwt:SecretKey"];
        if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, clientId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("client_id", clientId),
            new Claim("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}