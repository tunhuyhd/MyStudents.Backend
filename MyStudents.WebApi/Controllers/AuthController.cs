using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyStudents.Application.Auth.Commands;
using MyStudents.Application.Auth.Dto;
using MyStudents.Application.Auth.Queries;
using MyStudents.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Net.Http;

namespace MyStudents.WebApi.Controllers;

public class AuthController(
    IFileStorageService fileStorageService,
    IApplicationDbContext context,
    IWebHostEnvironment environment) : BaseApiController
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

    private static readonly HttpClient _httpClient = new();

    [HttpGet("avatar/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvatar(Guid userId, CancellationToken cancellationToken)
    {
        var user = await context.Users.FindAsync([userId], cancellationToken);
        if (user == null || string.IsNullOrEmpty(user.ImageUrl))
        {
            return NotFound("User or avatar not found.");
        }

        // 1. If it's a local storage path
        if (user.ImageUrl.StartsWith("/uploads/") || user.ImageUrl.StartsWith("uploads/"))
        {
            var wwwrootPath = environment.WebRootPath;
            if (string.IsNullOrEmpty(wwwrootPath))
            {
                wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            var physicalPath = Path.Combine(wwwrootPath, user.ImageUrl.TrimStart('/'));
            if (!System.IO.File.Exists(physicalPath))
            {
                return NotFound("Local avatar file not found.");
            }

            var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read);
            var contentType = GetContentType(physicalPath);
            Response.Headers.CacheControl = "public, max-age=86400"; // 1 day cache
            return File(stream, contentType);
        }

        // 2. If it's stored on Cloudinary
        try
        {
            var signedUrl = fileStorageService.GetShareableUrl(user.ImageUrl);
            var response = await _httpClient.GetAsync(signedUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Failed to retrieve avatar from Cloudinary.");
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
            Response.Headers.CacheControl = "public, max-age=86400"; // 1 day cache
            return File(responseStream, contentType);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving avatar: {ex.Message}");
        }
    }

    private static string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
