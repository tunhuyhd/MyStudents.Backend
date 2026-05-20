using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using MyStudents.Application.Common.Interfaces;

namespace MyStudents.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalFileStorageService(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
    {
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var wwwrootPath = _environment.WebRootPath;
        if (string.IsNullOrEmpty(wwwrootPath))
        {
            wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var uploadsFolder = Path.Combine(wwwrootPath, "uploads", "avatars");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);
        }

        return $"/uploads/avatars/{uniqueFileName}";
    }

    public Task<bool> DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            string path = fileUrl;
            if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
            {
                path = uri.AbsolutePath;
            }

            var wwwrootPath = _environment.WebRootPath;
            if (string.IsNullOrEmpty(wwwrootPath))
            {
                wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var physicalPath = Path.Combine(wwwrootPath, path.TrimStart('/'));

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
                return Task.FromResult(true);
            }
        }
        catch
        {
            // Ignore
        }
        return Task.FromResult(false);
    }

    public string GetShareableUrl(string? storedUrlOrPath)
    {
        return storedUrlOrPath ?? string.Empty;
    }
}
