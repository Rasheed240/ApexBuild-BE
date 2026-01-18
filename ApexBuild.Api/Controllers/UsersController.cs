using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Features.Users.Commands.InviteUser;
using ApexBuild.Application.Features.Users.Commands.AcceptInvite;
using ApexBuild.Application.Features.Users.Queries.GetProfileCompletion;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Contracts.Requests;
using ApexBuild.Contracts.Responses;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public UsersController(
            IMediator mediator,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Invite a user to the platform or project
        /// </summary>
        [HttpPost("invite")]
        [ProducesResponseType(typeof(ApiResponse<InviteUserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<InviteUserResponse>>> InviteUser([FromBody] InviteUserCommand command)
        {
            // Set the inviter as the current user
            var commandWithInviter = command with
            {
                InvitedByUserId = _currentUserService.UserId ?? Guid.Empty
            };

            var response = await _mediator.Send(commandWithInviter);
            return Ok(ApiResponse.Success(response, "User invitation sent successfully"));
        }

        /// <summary>
        /// Accept an invitation (No authentication required)
        /// </summary>
        [HttpPost("accept-invite")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AcceptInviteResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<AcceptInviteResponse>>> AcceptInvite([FromBody] AcceptInviteCommand command)
        {
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, "Invitation accepted successfully"));
        }

        /// <summary>
        /// Get all users in a project
        /// </summary>
        [HttpGet("project/{projectId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<object>>> GetUsersByProject(Guid projectId)
        {
            var users = await _unitOfWork.Users.GetUsersByProjectAsync(projectId);

            var userDtos = users.Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                u.ProfileImageUrl,
                u.PhoneNumber,
                WorkInfo = u.WorkInfos.FirstOrDefault(w => w.ProjectId == projectId && w.IsActive)
            }).ToList();

            return Ok(ApiResponse.Success(userDtos, "Users retrieved successfully"));
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<object>>> GetUserById(Guid userId, [FromQuery] Guid? organizationId, [FromQuery] Guid? projectId)
        {
            var user = await _unitOfWork.Users.GetWithRolesAsync(userId);

            if (user == null)
            {
                throw new ApexBuild.Application.Common.Exceptions.NotFoundException("User", userId);
            }

            var activeRoles = user.UserRoles.Where(ur => ur.IsActive).AsEnumerable();

            if (projectId.HasValue)
            {
                activeRoles = activeRoles.Where(ur => ur.ProjectId == projectId.Value);
            }
            else if (organizationId.HasValue)
            {
                activeRoles = activeRoles.Where(ur =>
                    ur.OrganizationId == organizationId.Value ||
                    ur.Project?.OrganizationId == organizationId.Value);
            }

            var userDto = new
            {
                user.Id,
                user.Email,
                user.FullName,
                user.FirstName,
                user.LastName,
                user.MiddleName,
                user.ProfileImageUrl,
                user.PhoneNumber,
                user.Bio,
                user.Address,
                user.City,
                user.State,
                user.Country,
                user.DateOfBirth,
                user.Gender,
                user.Status,
                user.EmailConfirmed,
                user.TwoFactorEnabled,
                user.CreatedAt,
                Roles = activeRoles.Select(ur => new
                {
                    ur.RoleId,
                    ur.Role.Name,
                    ur.Role.RoleType,
                    ur.ProjectId,
                    ProjectName = ur.Project?.Name,
                    ur.OrganizationId,
                    OrganizationName = ur.Organization?.Name
                })
            };

            return Ok(ApiResponse.Success(userDto, "User retrieved successfully"));
        }

        /// <summary>
        /// Get pending invitations for current user's projects
        /// </summary>
        [HttpGet("invitations/pending")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<object>>> GetPendingInvitations()
        {
            // Get all projects where current user is admin/owner
            var projects = await _unitOfWork.Projects.GetProjectsByUserAsync(
                _currentUserService.UserId ?? Guid.Empty);

            var allInvitations = new List<object>();

            foreach (var project in projects)
            {
                var invitations = await _unitOfWork.Invitations.GetInvitationsByProjectAsync(project.Id);

                var invitationDtos = invitations.Select(i => new
                {
                    i.Id,
                    i.Email,
                    i.Status,
                    i.CreatedAt,
                    i.ExpiresAt,
                    i.AcceptedAt,
                    InvitedBy = i.InvitedByUser.FullName,
                    Role = i.Role.Name,
                    ProjectName = i.Project?.Name,
                    i.Position
                });

                allInvitations.AddRange(invitationDtos);
            }

            return Ok(ApiResponse.Success(allInvitations, "Pending invitations retrieved successfully"));
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        [HttpPut("profile")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<object>>> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                throw new ApexBuild.Application.Common.Exceptions.UnauthorizedException("User not authenticated");
            }

            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null)
            {
                throw new ApexBuild.Application.Common.Exceptions.NotFoundException("User", userId.Value);
            }

            user.FirstName = request.FirstName ?? user.FirstName;
            user.LastName = request.LastName ?? user.LastName;
            user.MiddleName = request.MiddleName ?? user.MiddleName;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
            user.Bio = request.Bio ?? user.Bio;
            user.ProfileImageUrl = request.ProfileImageUrl ?? user.ProfileImageUrl;
            user.DateOfBirth = request.DateOfBirth ?? user.DateOfBirth;
            user.Gender = request.Gender ?? user.Gender;
            user.Address = request.Address ?? user.Address;
            user.City = request.City ?? user.City;
            user.State = request.State ?? user.State;
            user.Country = request.Country ?? user.Country;

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            var userDto = new
            {
                id = user.Id,
                userId = user.Id.ToString(),
                user.Email,
                user.FullName,
                user.FirstName,
                user.LastName,
                user.MiddleName,
                user.ProfileImageUrl,
                user.PhoneNumber,
                user.Bio,
                user.Address,
                user.City,
                user.State,
                user.Country,
                user.DateOfBirth,
                user.Gender,
                user.Status,
                user.EmailConfirmed,
                user.TwoFactorEnabled,
                createdAt = user.CreatedAt,
                joinedAt = user.CreatedAt
            };

            return Ok(ApiResponse.Success(userDto, "Profile updated successfully"));
        }

        /// <summary>
        /// Get profile completion status
        /// </summary>
        [HttpGet("profile/completion")]
        [ProducesResponseType(typeof(ApiResponse<GetProfileCompletionResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<GetProfileCompletionResponse>>> GetProfileCompletion()
        {
            var response = await _mediator.Send(new GetProfileCompletionQuery());
            return Ok(ApiResponse.Success(response, "Profile completion retrieved successfully"));
        }
    }
}