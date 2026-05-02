using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class BusinessHoursProfile : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "UTC";
    public string WorkingDays { get; set; } = "1,2,3,4,5";
    public TimeOnly StartLocalTime { get; set; } = new(8, 0, 0);
    public TimeOnly EndLocalTime { get; set; } = new(17, 0, 0);
    public bool IsDefault { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public Tenant? Tenant { get; set; }
}
