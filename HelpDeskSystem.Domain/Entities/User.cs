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
    public bool IsPortalUser { get; set; } = false; // End-customer submitting via portal
    public bool IsMfaEnabled { get; set; }
    public string MfaSecret { get; set; } = string.Empty;
    public DateTime? MfaEnabledAtUtc { get; set; }

    /// <summary>Full display name combining first and last name.</summary>
    public string FullName
    {
        get => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
            ? Username
            : $"{FirstName} {LastName}".Trim();
        set
        {
            var parts = value?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries) ?? [];
            FirstName = parts.Length > 0 ? parts[0] : string.Empty;
            LastName   = parts.Length > 1 ? parts[1] : string.Empty;
        }
    }

    // Navigation
    public Tenant? Tenant { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
