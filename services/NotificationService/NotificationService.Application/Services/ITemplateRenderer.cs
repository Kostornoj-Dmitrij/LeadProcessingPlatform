namespace NotificationService.Application.Services;

/// <summary>
/// Интерфейс для рендеринга шаблонов уведомлений
/// </summary>
public interface ITemplateRenderer
{
    string Render(string template, Dictionary<string, string> variables);
}