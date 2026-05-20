using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Common.Interfaces;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MyStudents.Application.Auth.Queries;

public record GetAvatarQuery(Guid UserId) : IRequest<AvatarFileResult?>;

public record AvatarFileResult(Stream Stream, string ContentType);

public class GetAvatarQueryHandler : IRequestHandler<GetAvatarQuery, AvatarFileResult?>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private static readonly HttpClient _httpClient = new();

    public GetAvatarQueryHandler(
        IApplicationDbContext context, 
        IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
    }

    public async Task<AvatarFileResult?> Handle(GetAvatarQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null || string.IsNullOrEmpty(user.ImageUrl))
        {
            return null;
        }

        var imageUrl = user.ImageUrl;

        // Case 1: Local storage file
        if (imageUrl.StartsWith("/uploads/") || !imageUrl.StartsWith("http"))
        {
            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var filePath = Path.Combine(wwwrootPath, imageUrl.TrimStart('/'));
            if (!File.Exists(filePath))
            {
                return null;
            }

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var contentType = GetContentType(filePath);
            return new AvatarFileResult(fileStream, contentType);
        }

        // Case 2: Cloudinary secure URL proxy
        try
        {
            // Get the signed URL to authorize backend download
            var signedUrl = _fileStorageService.GetShareableUrl(imageUrl);
            
            using var response = await _httpClient.GetAsync(signedUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var memoryStream = new MemoryStream();
            await response.Content.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0; // Reset stream position for reading
            
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            
            return new AvatarFileResult(memoryStream, contentType);
        }
        catch
        {
            return null;
        }
    }

    private string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
