using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Application.Common.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;

namespace ApexBuild.Infrastructure.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private const string BaseFolder = "apexbuild";

        public CloudinaryService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<(string Url, string PublicId)> UploadImageAsync(
            Stream stream,
            string fileName,
            string? folder = null,
            string? publicId = null,
            CancellationToken cancellationToken = default)
        {
            if (stream == null)
            {
                throw new ArgumentException("Stream is null", nameof(stream));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name is required", nameof(fileName));
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");
            }

            var folderPath = string.IsNullOrEmpty(folder) ? $"{BaseFolder}/images" : $"{BaseFolder}/{folder}";

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folderPath,
                PublicId = publicId,
                Transformation = new Transformation()
                    .Quality("auto")
                    .FetchFormat("auto")
                    .Crop("limit")
                    .Width(2000)
                    .Height(2000)
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Image upload failed: {uploadResult.Error.Message}");
            }

            return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
        }

        public async Task<(string Url, string PublicId)> UploadVideoAsync(
            Stream stream,
            string fileName,
            string? folder = null,
            string? publicId = null,
            CancellationToken cancellationToken = default)
        {
            if (stream == null)
            {
                throw new ArgumentException("Stream is null", nameof(stream));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name is required", nameof(fileName));
            }

            // Validate file type
            var allowedExtensions = new[] { ".mp4", ".mov", ".avi", ".webm", ".mkv" };
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");
            }

            var folderPath = string.IsNullOrEmpty(folder) ? $"{BaseFolder}/videos" : $"{BaseFolder}/{folder}";

            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folderPath,
                PublicId = publicId
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Video upload failed: {uploadResult.Error.Message}");
            }

            return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
        }

        public async Task<(string Url, string PublicId)> UploadFileAsync(
            Stream stream,
            string fileName,
            string? folder = null,
            string? publicId = null,
            CancellationToken cancellationToken = default)
        {
            if (stream == null)
            {
                throw new ArgumentException("Stream is null", nameof(stream));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name is required", nameof(fileName));
            }

            // Validate file type
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv" };
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");
            }

            var folderPath = string.IsNullOrEmpty(folder) ? $"{BaseFolder}/files" : $"{BaseFolder}/{folder}";

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folderPath,
                PublicId = publicId
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"File upload failed: {uploadResult.Error.Message}");
            }

            return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
        }

        public async Task<bool> DeleteResourceAsync(
            string publicId,
            string resourceType = "image",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                throw new ArgumentException("Public ID cannot be null or empty", nameof(publicId));
            }

            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = resourceType switch
                {
                    "video" => ResourceType.Video,
                    "raw" => ResourceType.Raw,
                    _ => ResourceType.Image
                }
            };

            var result = await _cloudinary.DestroyAsync(deleteParams);
            return result.Result == "ok" || result.Result == "not found";
        }

        public string GenerateImageUrl(
            string publicId,
            int? width = null,
            int? height = null,
            string crop = "fill")
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                throw new ArgumentException("Public ID cannot be null or empty", nameof(publicId));
            }

            var transformation = new Transformation();

            if (width.HasValue)
            {
                transformation = transformation.Width(width.Value);
            }

            if (height.HasValue)
            {
                transformation = transformation.Height(height.Value);
            }

            if (!string.IsNullOrWhiteSpace(crop))
            {
                transformation = transformation.Crop(crop);
            }

            transformation = transformation.Quality("auto").FetchFormat("auto");

            return _cloudinary.Api.UrlImgUp.Transform(transformation).BuildUrl(publicId);
        }

        // Legacy methods for backwards compatibility

        public async Task<string> UploadImageAsync(Stream imageStream, string fileName)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, imageStream),
                Folder = "apexbuild/images",
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Image upload failed: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }

        public async Task<string> UploadVideoAsync(Stream videoStream, string fileName)
        {
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(fileName, videoStream),
                Folder = "apexbuild/videos"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Video upload failed: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }

        public async Task<List<string>> UploadMultipleAsync(List<Stream> streams, List<string> fileNames)
        {
            if (streams.Count != fileNames.Count)
            {
                throw new ArgumentException("Streams and file names count must match");
            }

            var uploadTasks = new List<Task<string>>();

            for (int i = 0; i < streams.Count; i++)
            {
                var extension = Path.GetExtension(fileNames[i]).ToLower();
                var isVideo = new[] { ".mp4", ".mov", ".avi", ".mkv" }.Contains(extension);

                uploadTasks.Add(isVideo
                    ? UploadVideoAsync(streams[i], fileNames[i])
                    : UploadImageAsync(streams[i], fileNames[i]));
            }

            return (await Task.WhenAll(uploadTasks)).ToList();
        }

        public async Task<bool> DeleteAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);
            return result.Result == "ok";
        }
    }
}