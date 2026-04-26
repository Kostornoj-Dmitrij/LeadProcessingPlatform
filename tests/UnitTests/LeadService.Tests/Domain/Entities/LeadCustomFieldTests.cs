using AutoFixture.NUnit4;
using LeadService.Domain.Entities;
using NUnit.Framework;

namespace LeadService.Tests.Domain.Entities;

/// <summary>
/// Тесты для LeadCustomField
/// </summary>
[Category("Domain")]
public class LeadCustomFieldTests
{
    [Test, AutoData]
    public void Constructor_WithValidData_ShouldCreateField(
        string fieldName,
        string fieldValue)
    {
        var field = new LeadCustomField(fieldName, fieldValue);

        Assert.That(field.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(field.FieldName, Is.EqualTo(fieldName));
        Assert.That(field.FieldValue, Is.EqualTo(fieldValue));
        Assert.That(field.LeadId, Is.EqualTo(Guid.Empty));
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Constructor_WithEmptyFieldName_ShouldThrow(string? fieldName)
    {
        var ex = Assert.Throws<ArgumentException>(() => 
            new LeadCustomField(fieldName!, "value"));

        Assert.That(ex.Message, Does.Contain("Field name cannot be empty"));
    }
}