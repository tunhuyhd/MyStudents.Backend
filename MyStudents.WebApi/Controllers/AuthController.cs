using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyStudents.Application.Auth.Commands;
using MyStudents.Application.Auth.Dto;
using MyStudents.Application.Auth.Queries;

namespace MyStudents.WebApi.Controllers;

public class AuthController : BaseApiController
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        return await Mediator.Send(new LoginCommand(request.Username, request.Password));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        return await Mediator.Send(new RegisterUserCommand(request));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> RefreshToken(RefreshTokenRequest request)
    {
        return await Mediator.Send(new RefreshTokenCommand(request));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        return await Mediator.Send(new GetCurrentUserQuery());
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<bool>> UpdateProfile(UpdateProfileCommand command)
    {
        return await Mediator.Send(command);
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<ActionResult<bool>> ChangePassword(ChangePasswordCommand command)
    {
        return await Mediator.Send(command);
    }
}
