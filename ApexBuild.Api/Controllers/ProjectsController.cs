using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Features.Projects.Commands.CreateProject;
using ApexBuild.Application.Features.Projects.Commands.UpdateProject;
using ApexBuild.Application.Features.Projects.Commands.DeleteProject;
using ApexBuild.Application.Features.Projects.Queries.GetProjectById;
using ApexBuild.Application.Features.Projects.Queries.GetProjectsByOwner;
using ApexBuild.Application.Features.Projects.Queries.GetProjectsByUser;
using ApexBuild.Application.Features.Projects.Queries.ListProjects;
using ApexBuild.Application.Features.Projects.Queries.GetProjectProgress;
using ApexBuild.Application.Features.Projects.Queries.GetTopProjectProgress;
using ApexBuild.Application.Features.Tasks.Queries.GetProjectTasks;
using ApexBuild.Application.Features.Tasks.Queries.GetPendingTaskUpdates;
using ApexBuild.Application.Features.Projects.Queries.GetProjectMembers;
using ApexBuild.Application.Features.Tasks.Commands.ReviewTaskUpdate;
using ApexBuild.Contracts.Responses;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProjectsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Create a new project
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CreateProjectResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<CreateProjectResponse>>> CreateProject([FromBody] CreateProjectCommand command)
        {
            var response = await _mediator.Send(command);
            return CreatedAtAction(
                nameof(GetProjectById),
                new { projectId = response.ProjectId },
                ApiResponse.Success(response, response.Message));
        }

        /// <summary>
        /// Get a project by ID
        /// </summary>
        [HttpGet("{projectId}")]
        [ProducesResponseType(typeof(ApiResponse<GetProjectByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<GetProjectByIdResponse>>> GetProjectById(Guid projectId)
        {
            var query = new GetProjectByIdQuery { ProjectId = projectId };
            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Project retrieved successfully"));
        }

        /// <summary>
        /// Update a project
        /// </summary>
        [HttpPut("{projectId}")]
        [ProducesResponseType(typeof(ApiResponse<UpdateProjectResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<UpdateProjectResponse>>> UpdateProject(
            Guid projectId,
            [FromBody] UpdateProjectCommand command)
        {
            var commandWithId = command with { ProjectId = projectId };
            var response = await _mediator.Send(commandWithId);
            return Ok(ApiResponse.Success(response, response.Message));
        }

        /// <summary>
        /// Delete a project (soft delete)
        /// </summary>
        [HttpDelete("{projectId}")]
        [ProducesResponseType(typeof(ApiResponse<DeleteProjectResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<DeleteProjectResponse>>> DeleteProject(Guid projectId)
        {
            var command = new DeleteProjectCommand { ProjectId = projectId };
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, response.Message));
        }

        /// <summary>
        /// Get projects by owner
        /// </summary>
        [HttpGet("owner/{ownerId?}")]
        [ProducesResponseType(typeof(ApiResponse<GetProjectsByOwnerResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<GetProjectsByOwnerResponse>>> GetProjectsByOwner(Guid? ownerId = null)
        {
            var query = new GetProjectsByOwnerQuery { OwnerId = ownerId };
            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Projects retrieved successfully"));
        }

        /// <summary>
        /// Get projects by user (projects where user is a member)
        /// </summary>
        [HttpGet("user/{userId?}")]
        [ProducesResponseType(typeof(ApiResponse<GetProjectsByUserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<GetProjectsByUserResponse>>> GetProjectsByUser(Guid? userId = null)
        {
            var query = new GetProjectsByUserQuery { UserId = userId };
            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Projects retrieved successfully"));
        }

        /// <summary>
        /// List projects with pagination and filtering
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<ListProjectsResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<ListProjectsResponse>>> ListProjects(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Domain.Enums.ProjectStatus? status = null,
            [FromQuery] string? projectType = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] Guid? ownerId = null)
        {
            var query = new ListProjectsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Status = status,
                ProjectType = projectType,
                SearchTerm = searchTerm,
                OwnerId = ownerId
            };

            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Projects retrieved successfully"));
        }

        /// <summary>
        /// Get top N project progress computed from tasks. Count must be between 1 and 100.
        /// </summary>
        [HttpGet("progress/top")]
        [ProducesResponseType(typeof(ApiResponse<GetTopProjectProgressResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<GetTopProjectProgressResponse>>> GetTopProjectProgress([FromQuery] int count = 3)
        {
            if (count < 1 || count > 100)
                return BadRequest(ApiResponse.Failure<object>("Count must be between 1 and 100"));

            var query = new ApexBuild.Application.Features.Projects.Queries.GetTopProjectProgress.GetTopProjectProgressQuery(count);
            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Project progress retrieved successfully"));
        }

        /// <summary>
        /// Get progress for a single project computed from its tasks
        /// </summary>
        [HttpGet("{projectId}/progress")]
        [ProducesResponseType(typeof(ApiResponse<ProjectProgressDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProjectProgressDto>>> GetProjectProgress(Guid projectId)
        {
            var query = new GetProjectProgressQuery(projectId);
            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Project progress retrieved successfully"));
        }

        /// <summary>
        /// Get all members (WorkInfo) for a project
        /// </summary>
        [HttpGet("{projectId}/members")]
        [ProducesResponseType(typeof(ApiResponse<GetProjectMembersResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<GetProjectMembersResponse>>> GetProjectMembers(
            Guid projectId,
            [FromQuery] Guid? departmentId = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = new GetProjectMembersQuery
            {
                ProjectId = projectId,
                DepartmentId = departmentId,
                SearchTerm = searchTerm,
                IsActive = isActive,
                PageNumber = pageNumber,
                PageSize = pageSize,
            };
            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Project members retrieved successfully"));
        }

        /// <summary>
        /// Get all tasks for a project with subtasks and updates
        /// </summary>
        [HttpGet("{projectId}/tasks")]
        [ProducesResponseType(typeof(ApiResponse<GetProjectTasksResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<GetProjectTasksResponse>>> GetProjectTasks(
            Guid projectId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] Domain.Enums.TaskStatus? status = null,
            [FromQuery] int? priority = null,
            [FromQuery] Guid? departmentId = null,
            [FromQuery] Guid? assignedToUserId = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool includeSubtasks = true,
            [FromQuery] bool includeUpdates = true)
        {
            var query = new GetProjectTasksQuery
            {
                ProjectId = projectId,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Status = status,
                Priority = priority,
                DepartmentId = departmentId,
                AssignedToUserId = assignedToUserId,
                SearchTerm = searchTerm,
                IncludeSubtasks = includeSubtasks,
                IncludeUpdates = includeUpdates
            };

            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Project tasks retrieved successfully"));
        }

        /// <summary>
        /// Get pending task update reviews for current user
        /// </summary>
        [HttpGet("tasks/updates/pending")]
        [ProducesResponseType(typeof(ApiResponse<GetPendingTaskUpdatesResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<GetPendingTaskUpdatesResponse>>> GetPendingReviews(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] UpdateStatus? filterByStatus = null,
            [FromQuery] Guid? filterByProjectId = null,
            [FromQuery] Guid? filterByDepartmentId = null,
            [FromQuery] string? searchTerm = null)
        {
            var query = new GetPendingTaskUpdatesQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                FilterByStatus = filterByStatus,
                FilterByProjectId = filterByProjectId,
                FilterByDepartmentId = filterByDepartmentId,
                SearchTerm = searchTerm
            };

            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Pending reviews retrieved successfully"));
        }

        /// <summary>
        /// Review (approve or reject) a task update. Action must be "approve" or "reject".
        /// </summary>
        [HttpPost("tasks/updates/{taskUpdateId}/review")]
        [ProducesResponseType(typeof(ApiResponse<ReviewTaskUpdateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<ReviewTaskUpdateResponse>>> ReviewTaskUpdate(
            [FromRoute] Guid taskUpdateId,
            [FromBody] ReviewTaskUpdateRequest request)
        {
            if (request == null)
                return BadRequest(ApiResponse.Failure<object>("Request body is required"));

            if (string.IsNullOrWhiteSpace(request.Action))
                return BadRequest(ApiResponse.Failure<object>("Action is required (must be 'approve' or 'reject')"));

            var action = request.Action.ToLowerInvariant() == "approve" ? ReviewAction.Approve : ReviewAction.Reject;

            var command = new ReviewTaskUpdateCommand
            {
                TaskUpdateId = taskUpdateId,
                Action = action,
                ReviewNotes = request.ReviewNotes ?? "",
                AdjustedProgressPercentage = request.AdjustedProgressPercentage
            };

            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, response.Message));
        }
    }
}

public class ReviewTaskUpdateRequest
{
    public string Action { get; set; } = string.Empty;
    public string ReviewNotes { get; set; } = string.Empty;
    public decimal? AdjustedProgressPercentage { get; set; }
}

