using AutoFixture.NUnit4;
using LeadService.Domain.ValueObjects;
using NUnit.Framework;

namespace LeadService.Tests.Domain.ValueObjects;

/// <summary>
/// Тесты для CompanyName
/// </summary>
[Category("Domain")]
public class CompanyNameTests
{
    [Test, AutoData]
    public void Create_WithValidName_ShouldReturnCompanyName(
        string name)
    {
        var result = CompanyName.Create(name);

        Assert.That(result.Value, Is.EqualTo(name.Trim()));
    }

    [Test, AutoData]
    public void Create_WithNameWithSpaces_ShouldTrim(
        string name)
    {
        var nameWithSpaces = "       " + name + "       ";

        var result = CompanyName.Create(nameWithSpaces);

        Assert.That(result.Value, Is.EqualTo(name));
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Create_WithEmptyName_ShouldThrow(string? name)
    {
        var ex = Assert.Throws<ArgumentException>(() => CompanyName.Create(name!));
        Assert.That(ex.Message, Does.Contain("Company name cannot be empty"));
    }

    [Test]
    public void Create_WithNameTooLong_ShouldThrow()
    {
        var name = new string('a', 256);

        var ex = Assert.Throws<ArgumentException>(() => CompanyName.Create(name));
        Assert.That(ex.Message, Does.Contain("Company name cannot exceed 255 characters"));
    }

    [Test, AutoData]
    public void CreateUnsafe_ShouldNotValidate(
        string name)
    {
        var nameWithSpaces = "       " + name + "       ";

        var result = CompanyName.CreateUnsafe(nameWithSpaces);

        Assert.That(result.Value, Is.EqualTo(nameWithSpaces));
    }

    [Test, AutoData]
    public void Equality_WithSameValue_ShouldBeEqual(
        string name)
    {
        var companyName1 = CompanyName.Create(name);
        var companyName2 = CompanyName.Create(name);

        Assert.That(companyName1, Is.EqualTo(companyName2));
        Assert.That(companyName1 == companyName2, Is.True);
        Assert.That(companyName1 != companyName2, Is.False);
        Assert.That(companyName1.GetHashCode(), Is.EqualTo(companyName2.GetHashCode()));
    }

    [Test, AutoData]
    public void Equality_WithDifferentValue_ShouldNotBeEqual(
        string name1, string name2)
    {
        var companyName1 = CompanyName.Create(name1);
        var companyName2 = CompanyName.Create(name2);

        Assert.That(name1, Is.Not.EqualTo(name2));
        Assert.That(companyName1 == companyName2, Is.False);
        Assert.That(companyName1 != companyName2, Is.True);
    }

    [Test, AutoData]
    public void ToString_ShouldReturnValue(
        string name)
    {
        var companyName = CompanyName.Create(name);

        var result = companyName.ToString();

        Assert.That(result, Is.EqualTo(name));
    }
}