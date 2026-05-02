namespace HelpDeskSystem.API.Services;

public class RefreshTokenRotateResult
{
    public RefreshTokenRotateStatus Status { get; set; }
    public int UserId { get; set; }
    public string NewRefreshToken { get; set; } = string.Empty;
    public DateTime NewRefreshTokenExpiresAtUtc { get; set; }
}
