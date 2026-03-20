using LeadService.Domain.ValueObjects;
using NUnit.Framework;

namespace LeadService.Tests.Domain.ValueObjects;

/// <summary>
/// Тесты для Phone
/// </summary>
[Category("Domain")]
public class PhoneTests
{
    private const string ValidPhone = "+79001234567";
    private const string AnotherValidPhone = "+79007654321";

    [Test]
    [TestCase("+79001234567")]
    [TestCase("89001234567")]
    [TestCase("9001234567")]
    [TestCase("+7 (900) 123-45-67")]
    [TestCase("8-900-123-45-67")]
    public void Create_WithValidPhone_ShouldReturnPhone(string phone)
    {
        var result = Phone.Create(phone);

        Assert.That(result.Value, Is.EqualTo(phone.Trim()));
    }

    [Test]
    public void Create_WithPhoneWithSpaces_ShouldTrim()
    {
        var phone = "     " + ValidPhone + "     ";

        var result = Phone.Create(phone);

        Assert.That(result.Value, Is.EqualTo(ValidPhone));
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Create_WithEmptyPhone_ShouldThrow(string? phone)
    {
        var ex = Assert.Throws<ArgumentException>(() => Phone.Create(phone!));
        Assert.That(ex.Message, Does.Contain("Phone cannot be empty"));
    }

    [Test]
    [TestCase("123")]
    [TestCase("1234")]
    [TestCase("1234567890123456")]
    public void Create_WithInvalidLength_ShouldThrow(string phone)
    {
        var ex = Assert.Throws<ArgumentException>(() => Phone.Create(phone));
        Assert.That(ex.Message, Does.Contain("Phone number must have between 5 and 15 digits"));
    }

    [Test]
    public void Create_WithNonDigitCharacters_ShouldExtractDigits()
    {
        var phone = "abc" + ValidPhone + "def";

        var result = Phone.Create(phone);

        Assert.That(result.Value, Is.EqualTo(phone.Trim()));
    }

    [Test]
    public void CreateUnsafe_ShouldNotValidate()
    {
        var phone = "     " + ValidPhone + "     ";

        var result = Phone.CreateUnsafe(phone);

        Assert.That(result.Value, Is.EqualTo(phone));
    }

    [Test]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        var phone1 = Phone.Create(ValidPhone);
        var phone2 = Phone.Create(ValidPhone);

        Assert.That(phone1, Is.EqualTo(phone2));
        Assert.That(phone1 == phone2, Is.True);
        Assert.That(phone1 != phone2, Is.False);
        Assert.That(phone1.GetHashCode(), Is.EqualTo(phone2.GetHashCode()));
    }

    [Test]
    public void Equality_WithDifferentValue_ShouldNotBeEqual()
    {
        var phone1 = Phone.Create(ValidPhone);
        var phone2 = Phone.Create(AnotherValidPhone);

        Assert.That(phone1, Is.Not.EqualTo(phone2));
        Assert.That(phone1 == phone2, Is.False);
        Assert.That(phone1 != phone2, Is.True);
    }

    [Test]
    public void ToString_ShouldReturnOriginalValue()
    {
        var phone = Phone.Create(ValidPhone);

        var result = phone.ToString();

        Assert.That(result, Is.EqualTo(ValidPhone));
    }
}