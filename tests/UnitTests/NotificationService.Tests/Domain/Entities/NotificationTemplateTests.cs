using AutoFixture.NUnit3;
using NUnit.Framework;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Tests.Domain.Entities;

/// <summary>
/// Тесты для NotificationTemplate
/// </summary>
[Category("Domain")]
public class NotificationTemplateTests
{
    [Test, AutoData]
    public void Create_WithValidData_ShouldCreateTemplate(
        Guid id,
        string templateType,
        NotificationChannel channel,
        string subjectTemplate,
        string bodyTemplate,
        List<string> variables)
    {
        var template = NotificationTemplate.Create(
            id,
            templateType,
            channel,
            subjectTemplate,
            bodyTemplate,
            variables);

        Assert.That(template.Id, Is.EqualTo(id));
        Assert.That(template.TemplateType, Is.EqualTo(templateType));
        Assert.That(template.Channel, Is.EqualTo(channel));
        Assert.That(template.SubjectTemplate, Is.EqualTo(subjectTemplate));
        Assert.That(template.BodyTemplate, Is.EqualTo(bodyTemplate));
        Assert.That(template.Variables, Is.EqualTo(variables));
    }

    [Test, AutoData]
    public void Create_WithEmptyVariables_ShouldCreateWithEmptyList(
        Guid id,
        string templateType,
        NotificationChannel channel,
        string subjectTemplate,
        string bodyTemplate)
    {
        var template = NotificationTemplate.Create(
            id,
            templateType,
            channel,
            subjectTemplate,
            bodyTemplate,
            []);

        Assert.That(template.Variables, Is.Empty);
    }
}