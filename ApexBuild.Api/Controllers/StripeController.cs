using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Infrastructure.Configurations;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StripeController : ControllerBase
    {
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly StripeSettings _stripeSettings;
        private readonly ILogger<StripeController> _logger;

        public StripeController(
            IStripePaymentService stripePaymentService,
            IUnitOfWork unitOfWork,
            IOptions<StripeSettings> stripeSettings,
            ILogger<StripeController> logger)
        {
            _stripePaymentService = stripePaymentService;
            _unitOfWork = unitOfWork;
            _stripeSettings = stripeSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Creates a Stripe Checkout session for subscription creation
        /// </summary>
        [HttpPost("create-checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
        {
            try
            {
                // Validate organization exists
                var organization = await _unitOfWork.Organizations.GetByIdAsync(request.OrganizationId);
                if (organization == null)
                {
                    return NotFound(new { message = "Organization not found" });
                }

                // Get or create Stripe customer
                string customerId;
                if (string.IsNullOrEmpty(organization.StripeCustomerId))
                {
                    customerId = await _stripePaymentService.CreateCustomerAsync(
                        organization,
                        organization.Email ?? $"billing@{organization.Name.ToLower().Replace(" ", "")}.com",
                        organization.Name
                    );

                    organization.StripeCustomerId = customerId;
                    _unitOfWork.Organizations.Update(organization);
                    await _unitOfWork.SaveChangesAsync();
                }
                else
                {
                    customerId = organization.StripeCustomerId;
                }

                // Create Checkout Session
                var sessionService = new SessionService();
                var options = new SessionCreateOptions
                {
                    Customer = customerId,
                    PaymentMethodTypes = new List<string> { "card" },
                    Mode = "subscription",
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = _stripeSettings.MonthlyPriceId,
                            Quantity = request.NumberOfLicenses,
                        },
                    },
                    SubscriptionData = new SessionSubscriptionDataOptions
                    {
                        TrialPeriodDays = request.TrialDays > 0 ? request.TrialDays : null,
                        Metadata = new Dictionary<string, string>
                        {
                            { "organizationId", request.OrganizationId.ToString() },
                            { "numberOfLicenses", request.NumberOfLicenses.ToString() },
                        },
                    },
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        { "organizationId", request.OrganizationId.ToString() },
                    },
                };

                var session = await sessionService.CreateAsync(options);

                return Ok(new
                {
                    data = new
                    {
                        sessionId = session.Id,
                        url = session.Url,
                    }
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error creating checkout session");
                return BadRequest(new { message = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout session");
                return StatusCode(500, new { message = "Failed to create checkout session" });
            }
        }

        /// <summary>
        /// Creates a Stripe Setup session for adding payment methods
        /// </summary>
        [HttpPost("create-setup-session")]
        public async Task<IActionResult> CreateSetupSession([FromBody] CreateSetupSessionRequest request)
        {
            try
            {
                var sessionService = new SessionService();
                var options = new SessionCreateOptions
                {
                    Customer = request.CustomerId,
                    PaymentMethodTypes = new List<string> { "card" },
                    Mode = "setup",
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                };

                var session = await sessionService.CreateAsync(options);

                return Ok(new
                {
                    data = new
                    {
                        sessionId = session.Id,
                        url = session.Url,
                    }
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error creating setup session");
                return BadRequest(new { message = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating setup session");
                return StatusCode(500, new { message = "Failed to create setup session" });
            }
        }

        /// <summary>
        /// Gets all payment methods for an organization
        /// </summary>
        [HttpGet("payment-methods/{organizationId}")]
        public async Task<IActionResult> GetPaymentMethods(Guid organizationId)
        {
            try
            {
                var organization = await _unitOfWork.Organizations.GetByIdAsync(organizationId);
                if (organization == null)
                {
                    return NotFound(new { message = "Organization not found" });
                }

                if (string.IsNullOrEmpty(organization.StripeCustomerId))
                {
                    return Ok(new { data = new List<object>() });
                }

                var paymentMethodService = new PaymentMethodService();
                var options = new PaymentMethodListOptions
                {
                    Customer = organization.StripeCustomerId,
                    Type = "card",
                };

                var paymentMethods = await paymentMethodService.ListAsync(options);

                // Get customer to check default payment method
                var customerService = new CustomerService();
                var customer = await customerService.GetAsync(organization.StripeCustomerId);

                var result = paymentMethods.Data.Select(pm => new
                {
                    id = pm.Id,
                    card = new
                    {
                        brand = pm.Card.Brand,
                        last4 = pm.Card.Last4,
                        exp_month = pm.Card.ExpMonth,
                        exp_year = pm.Card.ExpYear,
                    },
                    isDefault = pm.Id == customer.InvoiceSettings?.DefaultPaymentMethod?.Id,
                }).ToList();

                return Ok(new { data = result });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error getting payment methods");
                return BadRequest(new { message = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment methods");
                return StatusCode(500, new { message = "Failed to get payment methods" });
            }
        }

        /// <summary>
        /// Sets a payment method as default
        /// </summary>
        [HttpPost("payment-methods/{paymentMethodId}/set-default")]
        public async Task<IActionResult> SetDefaultPaymentMethod(string paymentMethodId)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _unitOfWork.Users.GetByIdAsync(Guid.Parse(userId));
                if (user == null)
                {
                    return BadRequest(new { message = "User not found" });
                }
                
                var organization = await _unitOfWork.Organizations.GetByIdAsync(user.WorkInfos.FirstOrDefault()?.OrganizationId ?? Guid.Empty);
                
                if (organization == null || string.IsNullOrEmpty(organization.StripeCustomerId))
                {
                    return BadRequest(new { message = "No Stripe customer found" });
                }

                var customerService = new CustomerService();
                var options = new CustomerUpdateOptions
                {
                    InvoiceSettings = new CustomerInvoiceSettingsOptions
                    {
                        DefaultPaymentMethod = paymentMethodId,
                    },
                };

                await customerService.UpdateAsync(organization.StripeCustomerId, options);

                return Ok(new { data = new { success = true } });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error setting default payment method");
                return BadRequest(new { message = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default payment method");
                return StatusCode(500, new { message = "Failed to set default payment method" });
            }
        }

        /// <summary>
        /// Deletes a payment method
        /// </summary>
        [HttpDelete("payment-methods/{paymentMethodId}")]
        public async Task<IActionResult> DeletePaymentMethod(string paymentMethodId)
        {
            try
            {
                var paymentMethodService = new PaymentMethodService();
                await paymentMethodService.DetachAsync(paymentMethodId);

                return Ok(new { data = new { success = true } });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error deleting payment method");
                return BadRequest(new { message = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment method");
                return StatusCode(500, new { message = "Failed to delete payment method" });
            }
        }

        /// <summary>
        /// Gets Stripe publishable key for client-side
        /// </summary>
        [HttpGet("publishable-key")]
        [AllowAnonymous]
        public IActionResult GetPublishableKey()
        {
            return Ok(new
            {
                data = new
                {
                    publishableKey = _stripeSettings.PublishableKey,
                }
            });
        }

        /// <summary>
        /// Verifies checkout session and returns subscription info
        /// </summary>
        [HttpGet("verify-checkout/{sessionId}")]
        public async Task<IActionResult> VerifyCheckoutSession(string sessionId)
        {
            try
            {
                var sessionService = new SessionService();
                var session = await sessionService.GetAsync(sessionId, new SessionGetOptions
                {
                    Expand = new List<string> { "subscription" },
                });

                if (session.PaymentStatus != "paid" && session.Status != "complete")
                {
                    return BadRequest(new { message = "Payment not completed" });
                }

                return Ok(new
                {
                    data = new
                    {
                        success = true,
                        subscriptionId = session.SubscriptionId,
                        customerId = session.CustomerId,
                    }
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error verifying checkout session");
                return BadRequest(new { message = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying checkout session");
                return StatusCode(500, new { message = "Failed to verify checkout session" });
            }
        }

        /// <summary>
        /// Verifies setup session completion
        /// </summary>
        [HttpGet("verify-setup/{sessionId}")]
        public async Task<IActionResult> VerifySetupSession(string sessionId)
        {
            try
            {
                var sessionService = new SessionService();
                var session = await sessionService.GetAsync(sessionId, new SessionGetOptions
                {
                    Expand = new List<string> { "setup_intent" },
                });

                if (session.Status != "complete")
                {
                    return BadRequest(new { message = "Setup not completed" });
                }

                var setupIntent = session.SetupIntent as Stripe.SetupIntent;

                return Ok(new
                {
                    data = new
                    {
                        success = true,
                        paymentMethodId = setupIntent?.PaymentMethodId,
                    }
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error verifying setup session");
                return BadRequest(new { message = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying setup session");
                return StatusCode(500, new { message = "Failed to verify setup session" });
            }
        }
    }

    // Request DTOs
    public class CreateCheckoutSessionRequest
    {
        public Guid OrganizationId { get; set; }
        public int NumberOfLicenses { get; set; }
        public int TrialDays { get; set; }
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }

    public class CreateSetupSessionRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }
}
