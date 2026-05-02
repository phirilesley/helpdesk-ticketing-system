using HelpDeskSystem.Application.DTOs.Users;
using HelpDeskSystem.API.Security;

namespace HelpDeskSystem.API.Services;

public interface ITokenService
{
    TokenResult Generate(UserDto user);
}
