namespace HelpDeskSystem.Application.DTOs.Auth;

public class MfaEnrollmentDto
{
    public string SharedSecret { get; set; } = string.Empty;
    public string OtpauthUri { get; set; } = string.Empty;
}
