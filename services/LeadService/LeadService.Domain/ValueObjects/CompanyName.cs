using SharedKernel.Base;

namespace LeadService.Domain.ValueObjects;

/// <summary>
/// Value Object для названия компании.
/// </summary>
public sealed class CompanyName : ValueObject
{
    public string Value { get; }

    private CompanyName(string value)
    {
        Value = value;
    }

    public static CompanyName Create(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Company name cannot be empty.", nameof(companyName));

        if (companyName.Length > 255)
            throw new ArgumentException("Company name cannot exceed 255 characters.", nameof(companyName));

        return new CompanyName(companyName.Trim());
    }

    public static CompanyName CreateUnsafe(string companyName)
    {
        return new CompanyName(companyName);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}