namespace HelpDeskSystem.Application.DTOs.Auth;

public class RevokeTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
