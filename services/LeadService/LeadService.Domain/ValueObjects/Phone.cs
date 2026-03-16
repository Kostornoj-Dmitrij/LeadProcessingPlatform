using SharedKernel.Base;
using System.Text.RegularExpressions;

namespace LeadService.Domain.ValueObjects;

/// <summary>
/// Value Object для номера телефона.
/// </summary>
public sealed class Phone : ValueObject
{
    public string Value { get; }

    private Phone(string value)
    {
        Value = value;
    }

    public static Phone Create(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone cannot be empty.", nameof(phone));

        var digitsOnly = Regex.Replace(phone, @"[^\d]", "");

        if (digitsOnly.Length < 5 || digitsOnly.Length > 15)
            throw new ArgumentException("Phone number must have between 5 and 15 digits.", nameof(phone));

        return new Phone(phone.Trim());
    }

    public static Phone CreateUnsafe(string phone)
    {
        return new Phone(phone);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Regex.Replace(Value, @"[^\d]", "");
    }

    public override string ToString() => Value;
}