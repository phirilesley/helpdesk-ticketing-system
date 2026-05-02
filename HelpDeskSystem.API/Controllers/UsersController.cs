using HelpDeskSystem.API.Security;
using HelpDeskSystem.API.Services;
using HelpDeskSystem.Application.DTOs.Auth;
using HelpDeskSystem.Application.DTOs.Users;
using HelpDeskSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public UsersController(
        IUserService userService,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto dto)
    {
        var result = await _userService.CreateUserAsync(dto);
        return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();
        return user;
    }

    [HttpGet("email/{email}")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
    {
        var user = await _userService.GetUserByEmailAsync(email);
        if (user == null)
            return NotFound();
        return user;
    }

    [HttpGet("tenant/{tenantId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersByTenant(int tenantId)
    {
        var users = await _userService.GetUsersByTenantAsync(tenantId);
        return Ok(users);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserDto dto)
    {
        await _userService.UpdateUserAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await _userService.DeleteUserAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/roles/{roleId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AssignRole(int id, int roleId)
    {
        await _userService.AssignRoleToUserAsync(id, roleId);
        return NoContent();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        var isValid = await _userService.ValidatePasswordAsync(dto.Email, dto.Password);
        if (!isValid)
            return Unauthorized();

        var user = await _userService.GetUserByEmailAsync(dto.Email);
        if (user == null)
            return Unauthorized();

        var accessToken = _tokenService.Generate(user);
        var (refreshToken, refreshExpiresAtUtc) = await _refreshTokenService.CreateAsync(
            user.Id,
            GetDeviceId(),
            GetDeviceName(),
            GetIpAddress(),
            GetUserAgent(),
            HttpContext.RequestAborted);

        return Ok(new LoginResponseDto
        {
            AccessToken = accessToken.AccessToken,
            ExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiresAtUtc,
            User = user
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return Unauthorized();

        var rotated = await _refreshTokenService.RotateAsync(
            dto.RefreshToken,
            GetDeviceId(),
            GetDeviceName(),
            GetIpAddress(),
            GetUserAgent(),
            HttpContext.RequestAborted);

        if (rotated.Status == RefreshTokenRotateStatus.ReuseDetected)
            return Unauthorized("Refresh token reuse detected. Session family invalidated.");

        if (rotated.Status != RefreshTokenRotateStatus.Success)
            return Unauthorized();

        var user = await _userService.GetUserByIdAsync(rotated.UserId);
        if (user == null)
            return Unauthorized();

        var accessToken = _tokenService.Generate(user);

        return Ok(new LoginResponseDto
        {
            AccessToken = accessToken.AccessToken,
            ExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = rotated.NewRefreshToken,
            RefreshTokenExpiresAtUtc = rotated.NewRefreshTokenExpiresAtUtc,
            User = user
        });
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return BadRequest("Refresh token is required.");

        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var revoked = await _refreshTokenService.RevokeAsync(dto.RefreshToken, userId.Value, HttpContext.RequestAborted);
        if (!revoked)
            return NotFound();

        return NoContent();
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyCollection<UserSessionInfo>>> GetSessions()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var sessions = await _refreshTokenService.GetSessionsAsync(userId.Value, HttpContext.RequestAborted);
        return Ok(sessions);
    }

    [HttpPost("sessions/{familyId}/revoke")]
    [Authorize]
    public async Task<IActionResult> RevokeSessionFamily(string familyId)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var revoked = await _refreshTokenService.RevokeSessionFamilyAsync(userId.Value, familyId, HttpContext.RequestAborted);
        if (!revoked)
            return NotFound();

        return NoContent();
    }

    [HttpPost("sessions/revoke-all")]
    [Authorize]
    public async Task<IActionResult> RevokeAllSessions()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        await _refreshTokenService.RevokeAllSessionsAsync(userId.Value, HttpContext.RequestAborted);
        return NoContent();
    }

    private string GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private string GetUserAgent() => Request.Headers.UserAgent.ToString();

    private string GetDeviceId() => Request.Headers["X-Device-Id"].ToString();

    private string GetDeviceName() => Request.Headers["X-Device-Name"].ToString();
}
