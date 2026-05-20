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
                await fileStorageService.DeleteAsync(user.ImageUrl, cancellationToken);
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
        user.LastModifiedOn = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return $"/api/v1/auth/avatar/{user.Id}?v={(user.LastModifiedOn ?? user.CreatedOn).Ticks}";
    }
}
