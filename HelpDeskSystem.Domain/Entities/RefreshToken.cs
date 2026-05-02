using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public string FamilyId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
    public string CreatedByUserAgent { get; set; } = string.Empty;
    public string? ReplacedByTokenHash { get; set; }

    public User? User { get; set; }
}
