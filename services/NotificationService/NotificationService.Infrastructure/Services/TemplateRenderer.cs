using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Сервис рендеринга шаблонов
/// </summary>
public class TemplateRenderer(ILogger<TemplateRenderer> logger) : ITemplateRenderer
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    public string Render(string template, Dictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return PlaceholderRegex.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            if (variables.TryGetValue(key, out var value))
                return value;

            logger.LogWarning("Placeholder {{ {Key} }} not found in variables", key);
            return match.Value;
        });
    }
}