using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int? TenantId { get; set; } // For multi-tenant
    public bool IsSuperAdmin { get; set; } = false;
    public bool IsMfaEnabled { get; set; }
    public string MfaSecret { get; set; } = string.Empty;
    public DateTime? MfaEnabledAtUtc { get; set; }

    // Navigation
    public Tenant? Tenant { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
