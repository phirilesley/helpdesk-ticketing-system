namespace HelpDeskSystem.Application.DTOs.Auth;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string MfaCode { get; set; } = string.Empty;
}
