using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Features.Milestones.Commands;
using ApexBuild.Application.Features.Milestones.Queries;
using ApexBuild.Contracts.Responses;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MilestonesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MilestonesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get all milestones for a project, ordered by sequence.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpGet("project/{projectId}")]
        [ProducesResponseType(typeof(ApiResponse<List<MilestoneDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<List<MilestoneDto>>>> GetByProject(Guid projectId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMilestonesByProjectQuery(projectId), cancellationToken);
            return Ok(ApiResponse.Success<List<MilestoneDto>>(result));
        }

        /// <summary>
        /// Create a new project milestone.
        /// </summary>
        /// <param name="command">The milestone creation request payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CreateMilestoneResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<CreateMilestoneResponse>>> Create([FromBody] CreateMilestoneCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse.Success<CreateMilestoneResponse>(result, "Milestone created successfully."));
        }

        /// <summary>
        /// Mark a milestone as completed.
        /// </summary>
        /// <param name="milestoneId">The unique identifier of the milestone to complete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPost("{milestoneId}/complete")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Complete(Guid milestoneId, CancellationToken cancellationToken)
        {
            await _mediator.Send(new CompleteMilestoneCommand(milestoneId), cancellationToken);
            return Ok(ApiResponse.Success<bool>(true, "Milestone marked as completed."));
        }
    }
}
