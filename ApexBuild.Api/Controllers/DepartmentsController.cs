using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Features.Departments.Commands;
using ApexBuild.Application.Features.Departments.Queries;
using ApexBuild.Contracts.Responses;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DepartmentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get all departments for a project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpGet("project/{projectId}")]
        [ProducesResponseType(typeof(ApiResponse<List<DepartmentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> GetByProject(Guid projectId, CancellationToken cancellationToken)
        {
            if (projectId == Guid.Empty)
            {
                return BadRequest(ApiResponse.Failure<object>("A valid project ID is required."));
            }

            var result = await _mediator.Send(new GetDepartmentsByProjectQuery(projectId), cancellationToken);
            return Ok(ApiResponse.Success<List<DepartmentDto>>(result));
        }

        /// <summary>
        /// Create a new department in a project.
        /// </summary>
        /// <param name="command">The department creation request payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CreateDepartmentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<CreateDepartmentResponse>>> Create([FromBody] CreateDepartmentCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse.Success<CreateDepartmentResponse>(result, "Department created successfully."));
        }
    }
}
