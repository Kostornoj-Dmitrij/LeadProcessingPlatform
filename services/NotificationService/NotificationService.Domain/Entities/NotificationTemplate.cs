using SharedKernel.Base;
using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Агрегат для хранения шаблонов уведомлений
/// </summary>
public class NotificationTemplate : Entity<Guid>, IAggregateRoot
{
    public string TemplateType { get; private set; } = string.Empty;
    public NotificationChannel Channel { get; private set; }
    public string SubjectTemplate { get; private set; } = string.Empty;
    public string BodyTemplate { get; private set; } = string.Empty;
    public List<string> Variables { get; private set; } = [];

    private NotificationTemplate(Guid id) : base(id) { }

    public static NotificationTemplate Create(
        Guid id,
        string templateType,
        NotificationChannel channel,
        string subjectTemplate,
        string bodyTemplate,
        List<string> variables)
    {
        return new NotificationTemplate(id)
        {
            TemplateType = templateType,
            Channel = channel,
            SubjectTemplate = subjectTemplate,
            BodyTemplate = bodyTemplate,
            Variables = variables
        };
    }
}