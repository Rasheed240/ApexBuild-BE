using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Features.Organizations.Commands.CreateOrganization;
using ApexBuild.Application.Features.Organizations.Commands.UpdateOrganization;
using ApexBuild.Application.Features.Organizations.Commands.DeleteOrganization;
using ApexBuild.Application.Features.Organizations.Commands.AddMember;
using ApexBuild.Application.Features.Organizations.Commands.RemoveMember;
using ApexBuild.Application.Features.Organizations.Queries.GetOrganizationById;
using ApexBuild.Application.Features.Organizations.Queries.GetOrganizationsByOwner;
using ApexBuild.Application.Features.Organizations.Queries.GetOrganizationMembers;
using ApexBuild.Application.Features.Organizations.Queries.ListOrganizations;
using ApexBuild.Application.Features.Organizations.Queries.ListOrganizations;
using ApexBuild.Application.Features.Organizations.Queries.GetOrganizationDepartments;
using ApexBuild.Contracts.Responses.DTOs;
using ApexBuild.Contracts.Responses;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrganizationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrganizationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Create a new organization
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CreateOrganizationResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<CreateOrganizationResponse>>> CreateOrganization([FromBody] CreateOrganizationCommand command)
        {
            var response = await _mediator.Send(command);
            return CreatedAtAction(
                nameof(GetOrganizationById),
                new { organizationId = response.OrganizationId },
                ApiResponse.Success(response, response.Message));
        }

        /// <summary>
        /// Get an organization by ID
        /// </summary>
        [HttpGet("{organizationId}")]
        [ProducesResponseType(typeof(ApiResponse<GetOrganizationByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<GetOrganizationByIdResponse>>> GetOrganizationById(Guid organizationId)
        {
            var query = new GetOrganizationByIdQuery { OrganizationId = organizationId };
            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Organization retrieved successfully"));
        }

        /// <summary>
        /// Update an organization
        /// </summary>
        [HttpPut("{organizationId}")]
        [ProducesResponseType(typeof(ApiResponse<UpdateOrganizationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<UpdateOrganizationResponse>>> UpdateOrganization(
            Guid organizationId,
            [FromBody] UpdateOrganizationCommand command)
        {
            var commandWithId = command with { OrganizationId = organizationId };
            var response = await _mediator.Send(commandWithId);
            return Ok(ApiResponse.Success(response, response.Message));
        }

        /// <summary>
        /// Delete an organization (soft delete)
        /// </summary>
        [HttpDelete("{organizationId}")]
        [ProducesResponseType(typeof(ApiResponse<DeleteOrganizationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<DeleteOrganizationResponse>>> DeleteOrganization(Guid organizationId)
        {
            var command = new DeleteOrganizationCommand { OrganizationId = organizationId };
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, response.Message));
        }

        /// <summary>
        /// Get organizations by owner
        /// </summary>
        [HttpGet("owner/{ownerId?}")]
        [ProducesResponseType(typeof(ApiResponse<GetOrganizationsByOwnerResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<GetOrganizationsByOwnerResponse>>> GetOrganizationsByOwner(Guid? ownerId = null)
        {
            var query = new GetOrganizationsByOwnerQuery { OwnerId = ownerId };
            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Organizations retrieved successfully"));
        }

        /// <summary>
        /// List organizations with pagination and filtering
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<ListOrganizationsResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<ListOrganizationsResponse>>> ListOrganizations(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool? isVerified = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] Guid? ownerId = null)
        {
            var query = new ListOrganizationsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                IsActive = isActive,
                IsVerified = isVerified,
                SearchTerm = searchTerm,
                OwnerId = ownerId
            };

            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Organizations retrieved successfully"));
        }

        /// <summary>
        /// Get members of an organization
        /// </summary>
        [HttpGet("{organizationId}/members")]
        [ProducesResponseType(typeof(ApiResponse<GetOrganizationMembersResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<GetOrganizationMembersResponse>>> GetOrganizationMembers(
            Guid organizationId,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? searchTerm = null)
        {
            var query = new GetOrganizationMembersQuery
            {
                OrganizationId = organizationId,
                IsActive = isActive,
                SearchTerm = searchTerm
            };
            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Members retrieved successfully"));
        }

        /// <summary>
        /// Get departments of an organization
        /// </summary>
        [HttpGet("{organizationId}/departments")]
        [ProducesResponseType(typeof(ApiResponse<List<DepartmentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> GetOrganizationDepartments(Guid organizationId)
        {
            var query = new GetOrganizationDepartmentsQuery { OrganizationId = organizationId };
            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Departments retrieved successfully"));
        }

        /// <summary>
        /// Add a member to an organization
        /// </summary>
        [HttpPost("{organizationId}/members")]
        [ProducesResponseType(typeof(ApiResponse<AddMemberResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<AddMemberResponse>>> AddMember(
            Guid organizationId,
            [FromBody] AddMemberCommand command)
        {
            var commandWithOrgId = command with { OrganizationId = organizationId };
            var response = await _mediator.Send(commandWithOrgId);
            return Ok(ApiResponse.Success(response, response.Message));
        }

        /// <summary>
        /// Remove a member from an organization
        /// </summary>
        [HttpDelete("{organizationId}/members/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<RemoveMemberResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<RemoveMemberResponse>>> RemoveMember(
            Guid organizationId,
            Guid userId)
        {
            var command = new RemoveMemberCommand 
            { 
                OrganizationId = organizationId,
                UserId = userId
            };
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, response.Message));
        }
    }
}

