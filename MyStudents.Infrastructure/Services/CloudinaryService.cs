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

        return uploadResult.SecureUrl.ToString();
    }

    public async Task<bool> DeleteAsync(string fileUrlOrId, CancellationToken cancellationToken = default)
    {
        var publicId = fileUrlOrId;
        if (fileUrlOrId.Contains("cloudinary.com"))
        {
            var (pid, _) = ExtractCloudinaryInfo(fileUrlOrId);
            publicId = pid ?? fileUrlOrId;
        }

        var deletionParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deletionParams);
        return result.Result == "ok";
    }

    public string GetShareableUrl(string? storedUrlOrPath)
    {
        if (string.IsNullOrEmpty(storedUrlOrPath)) return string.Empty;

        if (storedUrlOrPath.Contains("cloudinary.com"))
        {
            var (publicId, actionType) = ExtractCloudinaryInfo(storedUrlOrPath);
            if (string.IsNullOrEmpty(publicId) || string.IsNullOrEmpty(actionType)) 
                return storedUrlOrPath;

            // Generate a signed URL for delivery based on the asset's storage type (upload vs authenticated)
            var signedUrl = _cloudinary.Api.UrlImgUp
                .Action(actionType)
                .Signed(true)
                .BuildUrl(publicId);

            return signedUrl;
        }

        return storedUrlOrPath;
    }

    private (string? PublicId, string? ActionType) ExtractCloudinaryInfo(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var segments = path.Split('/');
            
            var uploadIndex = Array.IndexOf(segments, "upload");
            string actionType = "upload";
            
            if (uploadIndex == -1)
            {
                uploadIndex = Array.IndexOf(segments, "authenticated");
                actionType = "authenticated";
            }
            if (uploadIndex == -1)
            {
                uploadIndex = Array.IndexOf(segments, "private");
                actionType = "private";
            }

            if (uploadIndex == -1 || segments.Length <= uploadIndex + 2)
            {
                return (null, null);
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
            var publicId = dotIndex == -1 ? publicIdWithExt : publicIdWithExt[..dotIndex];
            
            return (publicId, actionType);
        }
        catch
        {
            return (null, null);
        }
    }
}
