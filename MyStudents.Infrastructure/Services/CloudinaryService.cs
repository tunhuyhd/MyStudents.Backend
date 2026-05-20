using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using MyStudents.Application.Common.Interfaces;

namespace MyStudents.Infrastructure.Services;

public class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;
}

public class CloudinaryService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinarySettings _settings;

    public CloudinaryService(IOptions<CloudinarySettings> settings)
    {
        _settings = settings.Value;
        var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = _settings.Folder,
            Type = "authenticated" // Secure the asset on Cloudinary
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (uploadResult.Error != null)
        {
            throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        // Return relative path: folder + file name + format extension
        return $"{uploadResult.PublicId}.{uploadResult.Format}";
    }

    public async Task<bool> DeleteAsync(string fileUrlOrId, CancellationToken cancellationToken = default)
    {
        var publicId = fileUrlOrId;

        // If the path contains the full URL, extract the public ID
        if (fileUrlOrId.Contains("cloudinary.com"))
        {
            // Fallback parse for old URLs stored in DB
            var uri = new Uri(fileUrlOrId);
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
                var publicIdWithExt = string.Join("/", publicIdSegments);
                var dotIdx = publicIdWithExt.LastIndexOf('.');
                publicId = dotIdx == -1 ? publicIdWithExt : publicIdWithExt[..dotIdx];
            }
        }
        else
        {
            // It is our new relative path (folder + file + format extension)
            // Strip the extension because Cloudinary's Destroy API expects just the publicId
            var dotIndex = fileUrlOrId.LastIndexOf('.');
            if (dotIndex != -1)
            {
                publicId = fileUrlOrId[..dotIndex];
            }
        }

        var deletionParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deletionParams);
        return result.Result == "ok";
    }
}
