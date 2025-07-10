using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Features.Activities.Queries.GetRecentActivities;
using ApexBuild.Contracts.Responses;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ActivitiesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ActivitiesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("recent")]
        [ProducesResponseType(typeof(ApiResponse<GetRecentActivitiesResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<GetRecentActivitiesResponse>>> GetRecent([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetRecentActivitiesQuery(pageNumber, pageSize);
            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Recent activities retrieved successfully"));
        }
    }
}
