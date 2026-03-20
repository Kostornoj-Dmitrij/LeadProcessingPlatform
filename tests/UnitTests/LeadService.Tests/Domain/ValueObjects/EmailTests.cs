using LeadService.Domain.ValueObjects;
using NUnit.Framework;

namespace LeadService.Tests.Domain.ValueObjects;

/// <summary>
/// Тесты для Email
/// </summary>
[TestFixture]
[Category("Domain")]
public class EmailTests
{
    private const string ValidEmail = "test@example.com";

    [Test]
    public void Create_WithValidEmail_ShouldReturnEmail()
    {
        var result = Email.Create(ValidEmail);

        Assert.That(result.Value, Is.EqualTo(ValidEmail.Trim().ToLowerInvariant()));
    }

    [Test]
    public void Create_WithEmailWithSpaces_ShouldTrim()
    {
        var email = "       " + ValidEmail + "       ";

        var result = Email.Create(email);

        Assert.That(result.Value, Is.EqualTo(ValidEmail));
    }

    [Test]
    public void Create_WithUpperCaseEmail_ShouldConvertToLower()
    {
        var email = ValidEmail.ToUpper();

        var result = Email.Create(email);

        Assert.That(result.Value, Is.EqualTo(ValidEmail));
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Create_WithEmptyEmail_ShouldThrow(string? email)
    {
        var ex = Assert.Throws<ArgumentException>(() => Email.Create(email!));
        Assert.That(ex.Message, Does.Contain("Email cannot be empty"));
    }

    [Test]
    [TestCase("not-an-email")]
    [TestCase("missing@domain")]
    public void Create_WithInvalidFormat_ShouldThrow(string invalidEmail)
    {
        var ex = Assert.Throws<ArgumentException>(() => Email.Create(invalidEmail));
        Assert.That(ex.Message, Does.Contain("Invalid email format"));
    }

    [Test]
    public void CreateUnsafe_ShouldNotValidate()
    {
        var email = "       " + ValidEmail + "       ";

        var result = Email.CreateUnsafe(email);

        Assert.That(result.Value, Is.EqualTo(email));
    }

    [Test]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        var email1 = Email.Create(ValidEmail);
        var email2 = Email.Create(ValidEmail);

        Assert.That(email1, Is.EqualTo(email2));
        Assert.That(email1 == email2, Is.True);
        Assert.That(email1 != email2, Is.False);
        Assert.That(email1.GetHashCode(), Is.EqualTo(email2.GetHashCode()));
    }

    [Test]
    public void Equality_WithSameValueDifferentCase_ShouldBeEqual()
    {
        var email1 = Email.Create(ValidEmail);
        var email2 = Email.Create(ValidEmail.ToUpper());

        Assert.That(email1, Is.EqualTo(email2));
        Assert.That(email1 == email2, Is.True);
    }

    [Test]
    public void Equality_WithDifferentValue_ShouldNotBeEqual()
    {
        var email1 = Email.Create(ValidEmail);
        var email2 = Email.Create("2" + ValidEmail);

        Assert.That(email1, Is.Not.EqualTo(email2));
        Assert.That(email1 == email2, Is.False);
        Assert.That(email1 != email2, Is.True);
    }

    [Test]
    public void ToString_ShouldReturnValue()
    {
        var email = Email.Create(ValidEmail);

        var result = email.ToString();

        Assert.That(result, Is.EqualTo(ValidEmail));
    }
}