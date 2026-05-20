using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

    [HttpPut("avatar")]
    [Authorize]
    public async Task<ActionResult<string>> UpdateAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is empty");
        }

        // Validate size (5MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest("File size exceeds 5MB limit");
        }

        // Validate file type (must be image)
        if (!file.ContentType.StartsWith("image/"))
        {
            return BadRequest("File is not an image");
        }

        using var stream = file.OpenReadStream();
        var command = new UpdateAvatarCommand(stream, file.FileName, file.ContentType);
        var imageUrl = await Mediator.Send(command);
        return Ok(new { imageUrl });
    }

    [HttpGet("avatar")]
    [AllowAnonymous]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetAvatar([FromQuery] Guid userId)
    {
        var result = await Mediator.Send(new GetAvatarQuery(userId));
        if (result == null)
        {
            return NotFound("Avatar not found");
        }

        return File(result.Stream, result.ContentType);
    }
}
