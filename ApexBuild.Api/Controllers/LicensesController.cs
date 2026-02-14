using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Contracts.Responses;

namespace ApexBuild.Api.Controllers
{
    /// <summary>
    /// Provides billing seat information for organizations.
    /// Billing model: $20/active user/month. SuperAdmin projects are free.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LicensesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public LicensesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get active seat usage for an organization (active users in active projects).
        /// </summary>
        [HttpGet("organization/{organizationId}/seats")]
        public async Task<IActionResult> GetSeatUsage(Guid organizationId, CancellationToken cancellationToken)
        {
            var subscription = await _unitOfWork.Subscriptions.FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId, cancellationToken);

            if (subscription == null)
                return NotFound(ApiResponse.Failure<object>("No subscription found for this organization."));

            var activeUsers = await _unitOfWork.ProjectUsers.FindAsync(
                pu => pu.Project.OrganizationId == organizationId && pu.IsActive, cancellationToken);

            var seatCount = activeUsers.Select(pu => pu.UserId).Distinct().Count();
            var monthlyAmount = subscription.IsFreePlan ? 0 : seatCount * subscription.UserMonthlyRate;

            return Ok(ApiResponse.Success<object>(new
            {
                OrganizationId = organizationId,
                ActiveSeats = seatCount,
                UserMonthlyRate = subscription.UserMonthlyRate,
                IsFreePlan = subscription.IsFreePlan,
                EstimatedMonthlyAmount = monthlyAmount,
                SubscriptionStatus = subscription.Status.ToString(),
                BillingEndDate = subscription.BillingEndDate
            }));
        }
    }
}
