namespace HelpDeskSystem.Application.DTOs.Sla;

public class BusinessHoursProfileDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "UTC";
    public string WorkingDays { get; set; } = "1,2,3,4,5";
    public TimeOnly StartLocalTime { get; set; }
    public TimeOnly EndLocalTime { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}
