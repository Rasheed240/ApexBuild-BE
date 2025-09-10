using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LicensesController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IUnitOfWork _unitOfWork;

        public LicensesController(
            ISubscriptionService subscriptionService,
            IUnitOfWork unitOfWork)
        {
            _subscriptionService = subscriptionService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get all licenses for an organization.
        /// </summary>
        [HttpGet("organization/{organizationId}")]
        public async Task<ActionResult<List<OrganizationLicenseDto>>> GetOrganizationLicenses(Guid organizationId)
        {
            var licenses = await _subscriptionService.GetOrganizationLicensesAsync(organizationId);

            var dtos = licenses.Select(l => new OrganizationLicenseDto
            {
                Id = l.Id,
                LicenseKey = l.LicenseKey,
                UserEmail = l.User?.Email,
                UserName = l.User?.FullName,
                Status = l.Status.ToString(),
                LicenseType = l.LicenseType,
                ValidFrom = l.ValidFrom,
                ValidUntil = l.ValidUntil,
                IsActive = l.IsActive,
                IsExpired = l.IsExpired,
                DaysUntilExpiration = l.DaysUntilExpiration,
                AssignedAt = l.AssignedAt
            }).ToList();

            return Ok(dtos);
        }

        /// <summary>
        /// Get active licenses for an organization.
        /// </summary>
        [HttpGet("organization/{organizationId}/active")]
        public async Task<ActionResult<List<OrganizationLicenseDto>>> GetActiveOrganizationLicenses(Guid organizationId)
        {
            var licenses = await _subscriptionService.GetOrganizationLicensesAsync(organizationId, LicenseStatus.Active);

            var dtos = licenses.Where(l => l.IsActive).Select(l => new OrganizationLicenseDto
            {
                Id = l.Id,
                LicenseKey = l.LicenseKey,
                UserEmail = l.User?.Email,
                UserName = l.User?.FullName,
                Status = l.Status.ToString(),
                LicenseType = l.LicenseType,
                ValidFrom = l.ValidFrom,
                ValidUntil = l.ValidUntil,
                IsActive = l.IsActive,
                IsExpired = l.IsExpired,
                DaysUntilExpiration = l.DaysUntilExpiration,
                AssignedAt = l.AssignedAt
            }).ToList();

            return Ok(dtos);
        }

        /// <summary>
        /// Get licenses for current user across all organizations.
        /// </summary>
        [HttpGet("my-licenses")]
        public async Task<ActionResult<List<OrganizationLicenseDto>>> GetMyLicenses()
        {
            // Note: Would need to extract userId from claims
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());

            var licenses = await _subscriptionService.GetUserLicensesAsync(userId);

            var dtos = licenses.Select(l => new OrganizationLicenseDto
            {
                Id = l.Id,
                OrganizationName = l.Organization?.Name,
                LicenseKey = l.LicenseKey,
                Status = l.Status.ToString(),
                LicenseType = l.LicenseType,
                ValidFrom = l.ValidFrom,
                ValidUntil = l.ValidUntil,
                IsActive = l.IsActive,
                IsExpired = l.IsExpired,
                DaysUntilExpiration = l.DaysUntilExpiration,
                AssignedAt = l.AssignedAt
            }).ToList();

            return Ok(dtos);
        }

        /// <summary>
        /// Assign a license to a user.
        /// </summary>
        [HttpPost("assign")]
        public async Task<IActionResult> AssignLicense([FromBody] AssignLicenseRequest request)
        {
            if (request.OrganizationId == Guid.Empty || request.UserId == Guid.Empty)
            {
                return BadRequest("Organization ID and User ID are required");
            }

            var (success, license, errorMessage) = await _subscriptionService.AssignLicenseAsync(
                request.OrganizationId,
                request.UserId);

            if (!success)
            {
                return BadRequest(errorMessage);
            }

            var dto = new OrganizationLicenseDto
            {
                Id = license.Id,
                LicenseKey = license.LicenseKey,
                Status = license.Status.ToString(),
                ValidUntil = license.ValidUntil,
                IsActive = license.IsActive
            };

            return Created(string.Empty, dto);
        }

        /// <summary>
        /// Revoke a license.
        /// </summary>
        [HttpPost("{licenseId}/revoke")]
        public async Task<IActionResult> RevokeLicense(Guid licenseId, [FromBody] RevokeLicenseRequest request)
        {
            var (success, errorMessage) = await _subscriptionService.RevokeLicenseAsync(
                licenseId,
                request.Reason ?? "Revoked by administrator");

            if (!success)
            {
                return BadRequest(errorMessage);
            }

            return Ok(new { message = "License revoked successfully" });
        }

        /// <summary>
        /// Check if user has active license in organization.
        /// </summary>
        [HttpGet("check/{organizationId}/{userId}")]
        public async Task<ActionResult<LicenseCheckDto>> CheckLicense(Guid organizationId, Guid userId)
        {
            var (hasLicense, license) = await _subscriptionService.HasActiveLicenseAsync(organizationId, userId);

            return Ok(new LicenseCheckDto
            {
                HasActiveLicense = hasLicense,
                LicenseStatus = license?.Status.ToString() ?? "None",
                ValidUntil = license?.ValidUntil
            });
        }
    }

    public class OrganizationLicenseDto
    {
        public Guid Id { get; set; }
        public string LicenseKey { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string? UserName { get; set; }
        public string? OrganizationName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string LicenseType { get; set; } = string.Empty;
        public DateTime ValidFrom { get; set; }
        public DateTime ValidUntil { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public int DaysUntilExpiration { get; set; }
        public DateTime AssignedAt { get; set; }
    }

    public class AssignLicenseRequest
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
    }

    public class RevokeLicenseRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class LicenseCheckDto
    {
        public bool HasActiveLicense { get; set; }
        public string LicenseStatus { get; set; } = string.Empty;
        public DateTime? ValidUntil { get; set; }
    }
}
