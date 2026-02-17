using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Contracts.Responses;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MediaController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MediaController> _logger;

        public MediaController(
            ICloudinaryService cloudinaryService,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<MediaController> logger)
        {
            _cloudinaryService = cloudinaryService;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Upload profile picture for the current user
        /// </summary>
        [HttpPost("profile-picture")]
        [ProducesResponseType(typeof(ApiResponse<UploadMediaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<UploadMediaResponse>>> UploadProfilePicture(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponse.Failure<object>("No file provided"));
                }

                const long maxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
                if (file.Length > maxFileSizeBytes)
                {
                    return BadRequest(ApiResponse.Failure<object>(
                        $"File size ({file.Length / (1024 * 1024.0):F1} MB) exceeds the maximum allowed size of {maxFileSizeBytes / (1024 * 1024)} MB"));
                }

                var userId = _currentUserService.UserId;
                if (!userId.HasValue)
                {
                    return Unauthorized(ApiResponse.Failure<object>("User not authenticated"));
                }

                // Get current user
                var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
                if (user == null)
                {
                    return NotFound(ApiResponse.Failure<object>("User not found"));
                }

                // Delete old profile picture if exists
                if (!string.IsNullOrWhiteSpace(user.ProfileImagePublicId))
                {
                    await _cloudinaryService.DeleteResourceAsync(
                        user.ProfileImagePublicId,
                        "image",
                        cancellationToken);
                }

                // Upload new profile picture
                using var stream = file.OpenReadStream();
                var (url, publicId) = await _cloudinaryService.UploadImageAsync(
                    stream,
                    file.FileName,
                    "profile-pictures",
                    $"user_{userId}",
                    cancellationToken);

                // Update user
                user.ProfileImageUrl = url;
                user.ProfileImagePublicId = publicId;
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var response = new UploadMediaResponse
                {
                    Url = url,
                    PublicId = publicId,
                    MediaType = "image"
                };

                return Ok(ApiResponse.Success(response, "Profile picture uploaded successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid file upload attempt");
                return BadRequest(ApiResponse.Failure<object>(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile picture");
                return StatusCode(500, ApiResponse.Failure<object>("Failed to upload profile picture"));
            }
        }

        /// <summary>
        /// Upload organization logo
        /// </summary>
        [HttpPost("organization-logo/{organizationId}")]
        [ProducesResponseType(typeof(ApiResponse<UploadMediaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<UploadMediaResponse>>> UploadOrganizationLogo(
            Guid organizationId,
            IFormFile file,
            CancellationToken cancellationToken)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponse.Failure<object>("No file provided"));
                }

                const long maxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
                if (file.Length > maxFileSizeBytes)
                {
                    return BadRequest(ApiResponse.Failure<object>(
                        $"File size ({file.Length / (1024 * 1024.0):F1} MB) exceeds the maximum allowed size of {maxFileSizeBytes / (1024 * 1024)} MB"));
                }

                var userId = _currentUserService.UserId;
                if (!userId.HasValue)
                {
                    return Unauthorized(ApiResponse.Failure<object>("User not authenticated"));
                }

                // Get organization
                var organization = await _unitOfWork.Organizations.GetByIdAsync(organizationId, cancellationToken);
                if (organization == null)
                {
                    return NotFound(ApiResponse.Failure<object>("Organization not found"));
                }

                // Check if user is owner or admin of the organization
                var isOwner = organization.OwnerId == userId.Value;
                var isMember = await _unitOfWork.OrganizationMembers.FindAsync(
                    om => om.OrganizationId == organizationId && om.UserId == userId.Value && om.IsActive,
                    cancellationToken);

                if (!isOwner && !isMember.Any())
                {
                    return Forbid();
                }

                // Delete old logo if exists
                if (!string.IsNullOrWhiteSpace(organization.LogoPublicId))
                {
                    await _cloudinaryService.DeleteResourceAsync(
                        organization.LogoPublicId,
                        "image",
                        cancellationToken);
                }

                // Upload new logo
                using var stream = file.OpenReadStream();
                var (url, publicId) = await _cloudinaryService.UploadImageAsync(
                    stream,
                    file.FileName,
                    "organization-logos",
                    $"org_{organizationId}",
                    cancellationToken);

                // Update organization
                organization.LogoUrl = url;
                organization.LogoPublicId = publicId;
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var response = new UploadMediaResponse
                {
                    Url = url,
                    PublicId = publicId,
                    MediaType = "image"
                };

                return Ok(ApiResponse.Success(response, "Organization logo uploaded successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid file upload attempt");
                return BadRequest(ApiResponse.Failure<object>(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading organization logo");
                return StatusCode(500, ApiResponse.Failure<object>("Failed to upload organization logo"));
            }
        }

        /// <summary>
        /// Upload media for a project (images, videos, documents)
        /// </summary>
        [HttpPost("project/{projectId}")]
        [ProducesResponseType(typeof(ApiResponse<UploadMediaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<UploadMediaResponse>>> UploadProjectMedia(
            Guid projectId,
            IFormFile file,
            [FromQuery] string mediaType = "image",
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponse.Failure<object>("No file provided"));
                }

                var userId = _currentUserService.UserId;
                if (!userId.HasValue)
                {
                    return Unauthorized(ApiResponse.Failure<object>("User not authenticated"));
                }

                // Get project
                var project = await _unitOfWork.Projects.GetByIdAsync(projectId, cancellationToken);
                if (project == null)
                {
                    return NotFound(ApiResponse.Failure<object>("Project not found"));
                }

                // Check if user has access to the project
                var hasAccess = project.CreatedBy == userId.Value ||
                               project.ProjectOwnerId == userId.Value ||
                               project.ProjectAdminId == userId.Value;

                if (!hasAccess)
                {
                    var projectUser = await _unitOfWork.ProjectUsers.FindAsync(
                        pu => pu.ProjectId == projectId && pu.UserId == userId.Value,
                        cancellationToken);
                    hasAccess = projectUser.Any();
                }

                if (!hasAccess)
                {
                    return Forbid();
                }

                using var stream = file.OpenReadStream();
                string url, publicId;

                // Upload based on media type
                switch (mediaType.ToLower())
                {
                    case "image":
                        (url, publicId) = await _cloudinaryService.UploadImageAsync(
                            stream,
                            file.FileName,
                            $"projects/{projectId}/images",
                            null,
                            cancellationToken);
                        break;

                    case "video":
                        (url, publicId) = await _cloudinaryService.UploadVideoAsync(
                            stream,
                            file.FileName,
                            $"projects/{projectId}/videos",
                            null,
                            cancellationToken);
                        break;

                    case "document":
                        (url, publicId) = await _cloudinaryService.UploadFileAsync(
                            stream,
                            file.FileName,
                            $"projects/{projectId}/documents",
                            null,
                            cancellationToken);
                        break;

                    default:
                        return BadRequest(ApiResponse.Failure<object>("Invalid media type. Use 'image', 'video', or 'document'"));
                }

                // Update project media URLs
                if (mediaType.ToLower() == "image")
                {
                    project.ImageUrls ??= new List<string>();
                    if (!project.ImageUrls.Contains(url))
                    {
                        project.ImageUrls.Add(url);
                    }
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                var response = new UploadMediaResponse
                {
                    Url = url,
                    PublicId = publicId,
                    MediaType = mediaType
                };

                return Ok(ApiResponse.Success(response, "Media uploaded successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid file upload attempt");
                return BadRequest(ApiResponse.Failure<object>(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading project media");
                return StatusCode(500, ApiResponse.Failure<object>("Failed to upload media"));
            }
        }

        /// <summary>
        /// Upload media for a task (images, videos, documents)
        /// </summary>
        [HttpPost("task/{taskId}")]
        [ProducesResponseType(typeof(ApiResponse<UploadMediaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<UploadMediaResponse>>> UploadTaskMedia(
            Guid taskId,
            IFormFile file,
            [FromQuery] string mediaType = "image",
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponse.Failure<object>("No file provided"));
                }

                var userId = _currentUserService.UserId;
                if (!userId.HasValue)
                {
                    return Unauthorized(ApiResponse.Failure<object>("User not authenticated"));
                }

                // Get task
                var task = await _unitOfWork.Tasks.GetByIdAsync(taskId, cancellationToken);
                if (task == null)
                {
                    return NotFound(ApiResponse.Failure<object>("Task not found"));
                }

                // Check if user has access to the task
                var hasAccess = task.AssignedToUserId == userId.Value ||
                               task.AssignedByUserId == userId.Value ||
                               task.CreatedBy == userId.Value;

                if (!hasAccess)
                {
                    return Forbid();
                }

                using var stream = file.OpenReadStream();
                string url, publicId;

                // Upload based on media type
                switch (mediaType.ToLower())
                {
                    case "image":
                        (url, publicId) = await _cloudinaryService.UploadImageAsync(
                            stream,
                            file.FileName,
                            $"tasks/{taskId}/images",
                            null,
                            cancellationToken);

                        task.ImageUrls ??= new List<string>();
                        if (!task.ImageUrls.Contains(url))
                        {
                            task.ImageUrls.Add(url);
                        }
                        break;

                    case "video":
                        (url, publicId) = await _cloudinaryService.UploadVideoAsync(
                            stream,
                            file.FileName,
                            $"tasks/{taskId}/videos",
                            null,
                            cancellationToken);

                        task.VideoUrls ??= new List<string>();
                        if (!task.VideoUrls.Contains(url))
                        {
                            task.VideoUrls.Add(url);
                        }
                        break;

                    case "document":
                        (url, publicId) = await _cloudinaryService.UploadFileAsync(
                            stream,
                            file.FileName,
                            $"tasks/{taskId}/documents",
                            null,
                            cancellationToken);

                        task.AttachmentUrls ??= new List<string>();
                        if (!task.AttachmentUrls.Contains(url))
                        {
                            task.AttachmentUrls.Add(url);
                        }
                        break;

                    default:
                        return BadRequest(ApiResponse.Failure<object>("Invalid media type. Use 'image', 'video', or 'document'"));
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var response = new UploadMediaResponse
                {
                    Url = url,
                    PublicId = publicId,
                    MediaType = mediaType
                };

                return Ok(ApiResponse.Success(response, "Task media uploaded successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid file upload attempt");
                return BadRequest(ApiResponse.Failure<object>(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading task media");
                return StatusCode(500, ApiResponse.Failure<object>("Failed to upload task media"));
            }
        }

        /// <summary>
        /// Delete media by public ID
        /// </summary>
        [HttpDelete("{publicId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteMedia(
            string publicId,
            [FromQuery] string resourceType = "image",
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(publicId))
                {
                    return BadRequest(ApiResponse.Failure<object>("Public ID is required"));
                }

                var success = await _cloudinaryService.DeleteResourceAsync(
                    publicId,
                    resourceType,
                    cancellationToken);

                if (success)
                {
                    return Ok(ApiResponse.Success<object>(null, "Media deleted successfully"));
                }

                return BadRequest(ApiResponse.Failure<object>("Failed to delete media"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting media");
                return StatusCode(500, ApiResponse.Failure<object>("Failed to delete media"));
            }
        }
    }

    public class UploadMediaResponse
    {
        public string Url { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
    }
}
