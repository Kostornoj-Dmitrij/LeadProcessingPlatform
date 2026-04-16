namespace DistributionService.Domain.Constants;

/// <summary>
/// Ключи конфигурации правил распределения
/// </summary>
public static class RuleConfigKeys
{
    public const string Type = "type";
    public const string Target = "target";

    public const string MinScore = "min_score";
    public const string Thresholds = "thresholds";
    public const string DefaultTarget = "default_target";

    public const string Industry = "industry";

    public const string Range = "range";

    public const string Territories = "territories";
    public const string Specializations = "specializations";
    public const string Default = "default";

    public const string Targets = "targets";

    public const string CustomFieldIndustry = "industry";
    public const string CustomFieldCompanySize = "company_size";
    public const string CustomFieldWebsite = "website";
    public const string CustomFieldRevenueRange = "revenue_range";
}