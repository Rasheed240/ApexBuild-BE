using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Contracts.Responses;
using ApexBuild.Application.Features.Dashboard.Queries.GetDashboardStats;
using ApexBuild.Application.Features.Dashboard.Queries.GetDashboardMetrics;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get dashboard statistics for cards
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ApiResponse<GetDashboardStatsResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<GetDashboardStatsResponse>>> GetStats()
        {
            var query = new GetDashboardStatsQuery();
            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Dashboard stats retrieved successfully"));
        }

        /// <summary>
        /// Get dashboard performance metrics (Productivity, On-Time Delivery, Task Completion)
        /// </summary>
        [HttpGet("metrics")]
        [ProducesResponseType(typeof(ApiResponse<GetDashboardMetricsResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<GetDashboardMetricsResponse>>> GetMetrics()
        {
            var query = new GetDashboardMetricsQuery();
            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Dashboard metrics retrieved successfully"));
        }
    }
}
