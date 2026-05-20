using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Interfaces;

namespace MyStudents.Application.Auth.Commands;

public record UpdateAvatarCommand(Stream FileStream, string FileName, string ContentType) : IRequest<string>;

public class UpdateAvatarCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    IFileStorageService fileStorageService) : IRequestHandler<UpdateAvatarCommand, string>
{
    public async Task<string> Handle(UpdateAvatarCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId == null)
        {
            throw new UnauthorizedAccessException();
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        // If user already has an image, delete the old one to save space
        if (!string.IsNullOrEmpty(user.ImageUrl))
        {
            try
            {
                // For local files, we pass the absolute URL to DeleteAsync
                // For Cloudinary files, we extract the public ID
                if (user.ImageUrl.Contains("cloudinary.com"))
                {
                    var publicId = ExtractPublicIdFromUrl(user.ImageUrl);
                    if (!string.IsNullOrEmpty(publicId))
                    {
                        await fileStorageService.DeleteAsync(publicId, cancellationToken);
                    }
                }
                else
                {
                    await fileStorageService.DeleteAsync(user.ImageUrl, cancellationToken);
                }
            }
            catch
            {
                // Log warning but don't fail the upload of new image
            }
        }

        // Upload new image
        var imageUrl = await fileStorageService.UploadAsync(request.FileStream, request.FileName, cancellationToken);

        // Update database
        user.ImageUrl = imageUrl;
        await context.SaveChangesAsync(cancellationToken);

        return imageUrl;
    }

    private string? ExtractPublicIdFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath; // /demo/image/upload/v1570979139/mystudents/dev/avatars/sample.jpg
            var segments = path.Split('/');
            
            var uploadIndex = Array.IndexOf(segments, "upload");
            if (uploadIndex == -1 || segments.Length <= uploadIndex + 2)
            {
                return null;
            }

            // Public ID starts after the version segment (which starts with 'v')
            var versionIndex = uploadIndex + 1;
            if (segments[versionIndex].StartsWith('v') && segments.Length > versionIndex + 1)
            {
                versionIndex++;
            }

            var publicIdSegments = segments[versionIndex..];
            var publicIdWithExt = string.Join("/", publicIdSegments);
            
            var dotIndex = publicIdWithExt.LastIndexOf('.');
            return dotIndex == -1 ? publicIdWithExt : publicIdWithExt[..dotIndex];
        }
        catch
        {
            return null;
        }
    }
}
