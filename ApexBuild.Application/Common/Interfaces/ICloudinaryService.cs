namespace ApexBuild.Application.Common.Interfaces
{
    public interface ICloudinaryService
    {
        /// <summary>
        /// Upload an image to Cloudinary
        /// </summary>
        /// <param name="stream">The image stream to upload</param>
        /// <param name="fileName">The file name</param>
        /// <param name="folder">Optional folder path in Cloudinary</param>
        /// <param name="publicId">Optional custom public ID</param>
        /// <returns>Tuple of (Url, PublicId)</returns>
        Task<(string Url, string PublicId)> UploadImageAsync(
            Stream stream,
            string fileName,
            string? folder = null,
            string? publicId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Upload a video to Cloudinary
        /// </summary>
        /// <param name="stream">The video stream to upload</param>
        /// <param name="fileName">The file name</param>
        /// <param name="folder">Optional folder path in Cloudinary</param>
        /// <param name="publicId">Optional custom public ID</param>
        /// <returns>Tuple of (Url, PublicId)</returns>
        Task<(string Url, string PublicId)> UploadVideoAsync(
            Stream stream,
            string fileName,
            string? folder = null,
            string? publicId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Upload a document/file to Cloudinary
        /// </summary>
        /// <param name="stream">The file stream to upload</param>
        /// <param name="fileName">The file name</param>
        /// <param name="folder">Optional folder path in Cloudinary</param>
        /// <param name="publicId">Optional custom public ID</param>
        /// <returns>Tuple of (Url, PublicId)</returns>
        Task<(string Url, string PublicId)> UploadFileAsync(
            Stream stream,
            string fileName,
            string? folder = null,
            string? publicId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete a resource from Cloudinary
        /// </summary>
        /// <param name="publicId">The public ID of the resource to delete</param>
        /// <param name="resourceType">Type of resource (image, video, raw)</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteResourceAsync(
            string publicId,
            string resourceType = "image",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate a transformation URL for an image (resize, crop, etc)
        /// </summary>
        /// <param name="publicId">The public ID of the image</param>
        /// <param name="width">Optional width</param>
        /// <param name="height">Optional height</param>
        /// <param name="crop">Crop mode (fill, fit, scale, etc)</param>
        /// <returns>Transformed image URL</returns>
        string GenerateImageUrl(
            string publicId,
            int? width = null,
            int? height = null,
            string crop = "fill");

        // Legacy methods for backwards compatibility
        [Obsolete("Use UploadImageAsync with IFormFile instead")]
        Task<string> UploadImageAsync(Stream imageStream, string fileName);

        [Obsolete("Use UploadVideoAsync with IFormFile instead")]
        Task<string> UploadVideoAsync(Stream videoStream, string fileName);

        [Obsolete("Use multiple UploadImageAsync/UploadVideoAsync calls instead")]
        Task<List<string>> UploadMultipleAsync(List<Stream> streams, List<string> fileNames);

        [Obsolete("Use DeleteResourceAsync instead")]
        Task<bool> DeleteAsync(string publicId);
    }
}