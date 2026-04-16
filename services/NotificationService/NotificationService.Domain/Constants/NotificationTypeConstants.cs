namespace NotificationService.Domain.Constants;

/// <summary>
/// Константы для типов уведомлений
/// </summary>
public static class NotificationTypeConstants
{
    public const string LeadCreated = "LeadCreated";
    public const string LeadQualified = "LeadQualified";
    public const string LeadDistributed = "LeadDistributed";
    public const string LeadDistributedFinal = "LeadDistributedFinal";
    public const string LeadRejected = "LeadRejected";
    public const string LeadRejectedFinal = "LeadRejectedFinal";
    public const string LeadDistributionFailed = "LeadDistributionFailed";
    public const string LeadDistributionFailedFinal = "LeadDistributionFailedFinal";
}