using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Auth.Dto;
using MyStudents.Application.Common.Interfaces;

namespace MyStudents.Application.Auth.Queries;

public record GetCurrentUserQuery : IRequest<UserDto>;

public class GetCurrentUserQueryHandler(
    IApplicationDbContext context, 
    ICurrentUserService currentUserService) : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException();
        }

        var userId = currentUserService.UserId.Value;

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new Exception("User not found.");
        }

        return new UserDto(user.Id, user.Username, user.Email, user.FullName, user.Role.Name, GetRelativeCloudinaryPath(user.ImageUrl));
    }

    private static string? GetRelativeCloudinaryPath(string? url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        
        if (url.Contains("cloudinary.com"))
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var segments = path.Split('/');
                
                var uploadIndex = Array.IndexOf(segments, "upload");
                if (uploadIndex == -1) uploadIndex = Array.IndexOf(segments, "authenticated");
                if (uploadIndex == -1) uploadIndex = Array.IndexOf(segments, "private");

                if (uploadIndex != -1 && segments.Length > uploadIndex + 2)
                {
                    var versionIndex = uploadIndex + 1;
                    if (segments[versionIndex].StartsWith('v') && segments.Length > versionIndex + 1)
                    {
                        versionIndex++;
                    }
                    var publicIdSegments = segments[versionIndex..];
                    return string.Join("/", publicIdSegments);
                }
            }
            catch
            {
                // Fallback to original if parsing fails
            }
        }
        return url;
    }
}
