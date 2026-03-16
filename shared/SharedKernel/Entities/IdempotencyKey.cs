namespace SharedKernel.Entities;

/// <summary>
/// Для идемпотентности HTTP API
/// </summary>
public class IdempotencyKey
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public Guid? LeadId { get; set; }
    public byte[] RequestHash { get; set; } = [];
    public int? ResponseCode { get; set; }
    public string? ResponseBody { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LockedUntil { get; set; }
}