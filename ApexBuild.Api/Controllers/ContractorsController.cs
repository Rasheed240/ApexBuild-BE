using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Features.Contractors.Commands;
using ApexBuild.Application.Features.Contractors.Queries;
using ApexBuild.Contracts.Responses;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContractorsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ContractorsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get all contractors for a project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpGet("project/{projectId}")]
        [ProducesResponseType(typeof(ApiResponse<List<ContractorSummaryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<List<ContractorSummaryDto>>>> GetByProject(Guid projectId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new ListContractorsByProjectQuery(projectId), cancellationToken);
            return Ok(ApiResponse.Success<List<ContractorSummaryDto>>(result));
        }

        /// <summary>
        /// Get a contractor by ID with full details including team members.
        /// </summary>
        /// <param name="contractorId">The unique identifier of the contractor.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpGet("{contractorId}")]
        [ProducesResponseType(typeof(ApiResponse<ContractorDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ContractorDetailDto>>> GetById(Guid contractorId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetContractorByIdQuery(contractorId), cancellationToken);
            return Ok(ApiResponse.Success<ContractorDetailDto>(result));
        }

        /// <summary>
        /// Add a contractor company to a project.
        /// </summary>
        /// <param name="command">The contractor creation request payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CreateContractorResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<CreateContractorResponse>>> Create([FromBody] CreateContractorCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { contractorId = result.Id },
                ApiResponse.Success<CreateContractorResponse>(result, "Contractor added successfully."));
        }

        /// <summary>
        /// Update contractor details or status.
        /// </summary>
        /// <param name="contractorId">The unique identifier of the contractor to update.</param>
        /// <param name="command">The contractor update request payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPut("{contractorId}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<bool>>> Update(Guid contractorId, [FromBody] UpdateContractorCommand command, CancellationToken cancellationToken)
        {
            var updated = command with { ContractorId = contractorId };
            await _mediator.Send(updated, cancellationToken);
            return Ok(ApiResponse.Success<bool>(true, "Contractor updated successfully."));
        }

        /// <summary>
        /// Remove (soft delete) a contractor from a project.
        /// </summary>
        /// <param name="contractorId">The unique identifier of the contractor to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpDelete("{contractorId}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid contractorId, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteContractorCommand(contractorId), cancellationToken);
            return Ok(ApiResponse.Success<bool>(true, "Contractor removed successfully."));
        }
    }
}
