using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Features.Manuals.Queries.GetLatestManual;
using ApexBuild.Contracts.Responses;
using ApexBuild.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBuild.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ManualsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ICurrentUserService _currentUserService;

    public ManualsController(
        IMediator mediator,
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService,
        ICurrentUserService currentUserService)
    {
        _mediator             = mediator;
        _unitOfWork           = unitOfWork;
        _cloudinaryService    = cloudinaryService;
        _currentUserService   = currentUserService;
    }

    /// <summary>
    /// Upload a new user manual PDF (SuperAdmin / PlatformAdmin only).
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<GetLatestManualResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<GetLatestManualResponse>>> Upload(
        IFormFile file,
        [FromQuery] string title   = "ApexBuild User Manual",
        [FromQuery] string version = "1.0",
        CancellationToken cancellationToken = default)
    {
        // ── Auth: only SuperAdmin / PlatformAdmin ─────────────────────────
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse.Failure<object>("Not authenticated"));

        var userRoles = await _unitOfWork.UserRoles.FindAsync(
            ur => ur.UserId == userId.Value && ur.IsActive, cancellationToken);

        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        bool isPrivileged = false;
        foreach (var rid in roleIds)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(rid, cancellationToken);
            if (role != null && (role.Name == "SuperAdmin" || role.Name == "PlatformAdmin"))
            { isPrivileged = true; break; }
        }
        if (!isPrivileged)
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse.Failure<object>("Only SuperAdmin or PlatformAdmin can upload manuals"));

        // ── File validation ───────────────────────────────────────────────
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse.Failure<object>("No file provided"));

        const long maxBytes = 50 * 1024 * 1024; // 50 MB
        if (file.Length > maxBytes)
            return BadRequest(ApiResponse.Failure<object>("File exceeds maximum size of 50 MB"));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".pdf")
            return BadRequest(ApiResponse.Failure<object>("Only PDF files are accepted"));

        // ── Upload to Cloudinary ──────────────────────────────────────────
        using var stream = file.OpenReadStream();
        var (url, publicId) = await _cloudinaryService.UploadFileAsync(
            stream, file.FileName, "manuals", null, cancellationToken);

        // ── Persist record ────────────────────────────────────────────────
        var manual = new UserManual
        {
            Title           = title,
            Version         = version,
            FileUrl         = url,
            FilePublicId    = publicId,
            FileSizeBytes   = file.Length,
            UploadedByUserId = userId.Value,
            CreatedBy       = userId.Value,
        };
        await _unitOfWork.UserManuals.AddAsync(manual, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return the newly uploaded manual info
        var response = new GetLatestManualResponse
        {
            Id            = manual.Id,
            Title         = manual.Title,
            Version       = manual.Version,
            FileUrl       = manual.FileUrl,
            FileSizeBytes = manual.FileSizeBytes,
            UploadedAt    = manual.CreatedAt,
        };
        return Ok(ApiResponse.Success(response, "Manual uploaded successfully"));
    }

    /// <summary>
    /// Get the latest uploaded user manual (any authenticated user).
    /// </summary>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(ApiResponse<GetLatestManualResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<GetLatestManualResponse>>> GetLatest(
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetLatestManualQuery(), cancellationToken);
        if (result is null)
            return NotFound(ApiResponse.Failure<object>("No manual has been uploaded yet"));
        return Ok(ApiResponse.Success(result, "Manual retrieved successfully"));
    }
}
