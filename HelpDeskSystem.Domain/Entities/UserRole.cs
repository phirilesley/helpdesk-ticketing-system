using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class UserRole : BaseEntity
{
    public int UserId { get; set; }
    public int RoleId { get; set; }

    // Navigation
    public User? User { get; set; }
    public Role? Role { get; set; }
}