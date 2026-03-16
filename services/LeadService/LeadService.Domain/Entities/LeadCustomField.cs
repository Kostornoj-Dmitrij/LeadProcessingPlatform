using SharedKernel.Base;

namespace LeadService.Domain.Entities;

/// <summary>
/// Дополнительное поле лида (ключ-значение)
/// </summary>
public sealed class LeadCustomField : Entity<Guid>
{
    public Guid LeadId { get; private set; }
    public string FieldName { get; private set; }
    public string FieldValue { get; private set; }

    private LeadCustomField(Guid id) : base(id) { }

    public LeadCustomField(string fieldName, string fieldValue) : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentException("Field name cannot be empty.", nameof(fieldName));

        FieldName = fieldName;
        FieldValue = fieldValue;
    }

    private LeadCustomField() { }
}