using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using ApexBuild.Contracts.Responses.DTOs;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthorizationService _authorizationService;

        public SubscriptionsController(
            ISubscriptionService subscriptionService,
            IUnitOfWork unitOfWork,
            IAuthorizationService authorizationService)
        {
            _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }

        /// <summary>
        /// Helper method to map Subscription entity to DTO.
        /// </summary>
        private static SubscriptionDto MapToDto(Subscription subscription)
        {
            return subscription.ToDto();
        }

        /// <summary>
        /// Helper method to check if user is org admin.
        /// </summary>
        private async Task<bool> IsOrgAdminAsync(Guid organizationId)
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return false;
            }

            var org = await _unitOfWork.Organizations.GetByIdAsync(organizationId);
            if (org == null)
            {
                return false;
            }

            // Check if user is org owner or is an active member
            if (org.OwnerId == userGuid)
            {
                return true;
            }

            var isMember = await _unitOfWork.OrganizationMembers.IsMemberAsync(organizationId, userGuid);
            return isMember;
        }

        /// <summary>
        /// Create a new subscription for an organization.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SubscriptionDto>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            // Get current user ID
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized("User ID not found in token");
            }

            // Check authorization
            if (!await IsOrgAdminAsync(request.OrganizationId))
            {
                return Forbid("You must be an organization admin to create subscription");
            }

            var (success, subscription, errorMessage) = await _subscriptionService.CreateSubscriptionAsync(
                request.OrganizationId,
                userGuid,
                false,
                request.TrialDays ?? 14);

            if (!success)
            {
                return BadRequest(errorMessage);
            }

            var subscriptionDto = MapToDto(subscription);

            return CreatedAtAction(
                nameof(GetOrganizationSubscription),
                new { organizationId = request.OrganizationId },
                subscriptionDto);
        }

        /// <summary>
        /// Get subscription for an organization.
        /// </summary>
        [HttpGet("organization/{organizationId}")]
        public async Task<ActionResult<SubscriptionDto>> GetOrganizationSubscription(Guid organizationId)
        {
            var subscription = await _subscriptionService.GetSubscriptionAsync(organizationId);
            if (subscription == null)
            {
                return NotFound();
            }

            var subscriptionDto = MapToDto(subscription);
            return Ok(subscriptionDto);
        }

        /// <summary>
        /// Get subscription statistics for an organization.
        /// </summary>
        [HttpGet("organization/{organizationId}/stats")]
        public async Task<ActionResult<SubscriptionStatsDto>> GetSubscriptionStats(Guid organizationId)
        {
            var stats = await _subscriptionService.GetSubscriptionStatsAsync(organizationId);
            if (stats == null)
            {
                return NotFound();
            }

            return Ok(stats);
        }

        /// <summary>
        /// Upgrade subscription (add licenses).
        /// Requires organization admin role.
        /// </summary>
        [HttpPost("{subscriptionId}/upgrade")]
        public async Task<IActionResult> UpgradeSubscription(
            Guid subscriptionId,
            [FromBody] UpgradeSubscriptionRequest request)
        {
            if (request.AdditionalLicenses <= 0)
            {
                return BadRequest("Additional licenses must be greater than 0");
            }

            // Get subscription to verify org
            var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
            if (subscription == null)
            {
                return NotFound("Subscription not found");
            }

            // Check authorization
            if (!await IsOrgAdminAsync(subscription.OrganizationId))
            {
                return Forbid("You must be an organization admin to upgrade subscription");
            }

            await _subscriptionService.UpdateActiveUserCountAsync(subscription.OrganizationId);
            return Ok(new { message = "Subscription user count updated successfully" });
        }

        /// <summary>
        /// Downgrade subscription (remove licenses).
        /// Requires organization admin role.
        /// </summary>
        [HttpPost("{subscriptionId}/downgrade")]
        public async Task<IActionResult> DowngradeSubscription(
            Guid subscriptionId,
            [FromBody] DowngradeSubscriptionRequest request)
        {
            if (request.LicensesToRemove <= 0)
            {
                return BadRequest("Licenses to remove must be greater than 0");
            }

            // Get subscription to verify org
            var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
            if (subscription == null)
            {
                return NotFound("Subscription not found");
            }

            // Check authorization
            if (!await IsOrgAdminAsync(subscription.OrganizationId))
            {
                return Forbid("You must be an organization admin to downgrade subscription");
            }

            await _subscriptionService.UpdateActiveUserCountAsync(subscription.OrganizationId);
            return Ok(new { message = "Subscription user count updated successfully" });
        }

        /// <summary>
        /// Renew subscription.
        /// Requires organization admin role.
        /// </summary>
        [HttpPost("{subscriptionId}/renew")]
        public async Task<IActionResult> RenewSubscription(Guid subscriptionId)
        {
            // Get subscription to verify org
            var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
            if (subscription == null)
            {
                return NotFound("Subscription not found");
            }

            // Check authorization
            if (!await IsOrgAdminAsync(subscription.OrganizationId))
            {
                return Forbid("You must be an organization admin to renew subscription");
            }

            var (success, errorMessage) = await _subscriptionService.RenewSubscriptionAsync(subscriptionId);

            if (!success)
            {
                return BadRequest(errorMessage);
            }

            return Ok(new { message = "Subscription renewed successfully" });
        }

        /// <summary>
        /// Cancel subscription.
        /// Requires organization admin role.
        /// </summary>
        [HttpPost("{subscriptionId}/cancel")]
        public async Task<IActionResult> CancelSubscription(
            Guid subscriptionId,
            [FromBody] CancelSubscriptionRequest request)
        {
            // Get subscription to verify org
            var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
            if (subscription == null)
            {
                return NotFound("Subscription not found");
            }

            // Check authorization
            if (!await IsOrgAdminAsync(subscription.OrganizationId))
            {
                return Forbid("You must be an organization admin to cancel subscription");
            }

            var (success, errorMessage) = await _subscriptionService.CancelSubscriptionAsync(
                subscriptionId,
                request.Reason ?? "Cancelled by user");

            if (!success)
            {
                return BadRequest(errorMessage);
            }

            return Ok(new { message = "Subscription cancelled successfully" });
        }

        /// <summary>
        /// Reactivate cancelled subscription.
        /// Requires organization admin role.
        /// </summary>
        [HttpPost("{subscriptionId}/reactivate")]
        public async Task<IActionResult> ReactivateSubscription(Guid subscriptionId)
        {
            // Get subscription to verify org
            var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
            if (subscription == null)
            {
                return NotFound("Subscription not found");
            }

            // Check authorization
            if (!await IsOrgAdminAsync(subscription.OrganizationId))
            {
                return Forbid("You must be an organization admin to reactivate subscription");
            }

            var (success, errorMessage) = await _subscriptionService.ReactivateSubscriptionAsync(subscriptionId);

            if (!success)
            {
                return BadRequest(errorMessage);
            }

            return Ok(new { message = "Subscription reactivated successfully" });
        }

        /// <summary>
        /// Preview proration for subscription quantity change
        /// </summary>
        [HttpGet("{subscriptionId}/preview-proration")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PreviewProration(
            Guid subscriptionId,
            [FromQuery] int newQuantity)
        {
            if (newQuantity < 1)
                return BadRequest(new { message = "New quantity must be at least 1" });

            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
                if (subscription == null)
                {
                    return NotFound(new { message = "Subscription not found" });
                }

                // Check authorization
                if (!await IsOrgAdminAsync(subscription.OrganizationId))
                {
                    return Forbid("You must be an organization admin to preview proration");
                }

                // Calculate proration using Stripe
                var invoiceService = new Stripe.InvoiceService();
                var options = new Stripe.UpcomingInvoiceOptions
                {
                    Customer = subscription.StripeCustomerId,
                    Subscription = subscription.StripeSubscriptionId,
                    SubscriptionItems = new List<Stripe.InvoiceSubscriptionItemOptions>
                    {
                        new Stripe.InvoiceSubscriptionItemOptions
                        {
                            Id = subscription.StripeSubscriptionItemId,
                            Quantity = newQuantity,
                        },
                    },
                };

                var upcomingInvoice = await invoiceService.UpcomingAsync(options);

                var currentPeriodStart = subscription.StripeCurrentPeriodStart ?? subscription.BillingStartDate;
                var currentPeriodEnd = subscription.StripeCurrentPeriodEnd ?? subscription.BillingEndDate;
                var now = DateTime.UtcNow;
                var billingPeriodDays = (currentPeriodEnd - currentPeriodStart).Days;
                var daysElapsed = (now - currentPeriodStart).Days;
                var daysRemaining = (currentPeriodEnd - now).Days;

                // Calculate prorated amount (immediate charge for upgrade or credit for downgrade)
               var proratedAmount = 0m;
                foreach (var line in upcomingInvoice.Lines.Data)
                {
                    if (line.Proration == true)
                    {
                        proratedAmount += line.Amount / 100.0m;
                    }
                }

                return Ok(new
                {
                    data = new
                    {
                        currentQuantity = subscription.ActiveUserCount,
                        newQuantity,
                        proratedAmount = Math.Abs(proratedAmount),
                        nextInvoiceAmount = upcomingInvoice.Total / 100.0m,
                        nextBillingDate = upcomingInvoice.PeriodEnd,
                        currentPeriodElapsed = daysElapsed,
                        daysRemaining,
                        billingPeriodDays,
                    }
                });
            }
            catch (Stripe.StripeException)
            {
                return BadRequest(new { message = "Stripe error" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Failed to preview proration" });
            }
        }
    }

    public class CreateSubscriptionRequest
    {
        public Guid OrganizationId { get; set; }
        public int? TrialDays { get; set; }
    }

    public class UpgradeSubscriptionRequest
    {
        public int AdditionalLicenses { get; set; }
    }

    public class DowngradeSubscriptionRequest
    {
        public int LicensesToRemove { get; set; }
    }

    public class CancelSubscriptionRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public static class SubscriptionExtensions
    {
        public static SubscriptionDto ToDto(this Subscription subscription)
        {
            return new SubscriptionDto
            {
                Id = subscription.Id,
                OrganizationId = subscription.OrganizationId,
                UserId = subscription.UserId,
                NumberOfLicenses = subscription.ActiveUserCount,
                LicensesUsed = subscription.ActiveUserCount,
                LicenseCostPerMonth = subscription.UserMonthlyRate,
                TotalMonthlyAmount = subscription.TotalMonthlyAmount,
                Status = subscription.Status.ToString(),
                BillingCycle = subscription.BillingCycle.ToString(),
                BillingStartDate = subscription.BillingStartDate,
                BillingEndDate = subscription.BillingEndDate,
                NextBillingDate = subscription.NextBillingDate,
                RenewalDate = subscription.RenewalDate,
                StripeCustomerId = subscription.StripeCustomerId,
                StripeSubscriptionId = subscription.StripeSubscriptionId,
                StripeSubscriptionItemId = subscription.StripeSubscriptionItemId,
                StripePriceId = subscription.StripePriceId,
                StripePaymentMethodId = subscription.StripePaymentMethodId,
                StripeCurrentPeriodStart = subscription.StripeCurrentPeriodStart,
                StripeCurrentPeriodEnd = subscription.StripeCurrentPeriodEnd,
                AutoRenew = subscription.AutoRenew,
                Amount = subscription.Amount,
                CreatedOn = subscription.CreatedAt,
                CancelledAt = subscription.CancelledAt,
                CancellationReason = subscription.CancellationReason,
                IsTrialPeriod = subscription.IsTrialPeriod,
                TrialEndDate = subscription.TrialEndDate,
                RemainingLicenses = 0,
                IsActive = subscription.IsActive,
                HasExpired = subscription.HasExpired,
                IsLowOnLicenses = false
            };
        }
    }
}
