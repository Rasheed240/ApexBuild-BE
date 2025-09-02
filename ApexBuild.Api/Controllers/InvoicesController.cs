using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Stripe;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(
            IUnitOfWork unitOfWork,
            IStripePaymentService stripePaymentService,
            ILogger<InvoicesController> logger)
        {
            _unitOfWork = unitOfWork;
            _stripePaymentService = stripePaymentService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all invoices for an organization with filters
        /// </summary>
        [HttpGet("organization/{organizationId}")]
        public async Task<IActionResult> GetOrganizationInvoices(
            Guid organizationId,
            [FromQuery] string status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
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
                    return Ok(new
                    {
                        data = new
                        {
                            invoices = new List<object>(),
                            total = 0,
                            pages = 0,
                        }
                    });
                }

                var invoiceService = new InvoiceService();
                var options = new InvoiceListOptions
                {
                    Customer = organization.StripeCustomerId,
                    Limit = limit,
                };

                if (status != null)
                {
                    options.Status = status;
                }

                if (startDate.HasValue)
                {
                    options.Created = new DateRangeOptions
                    {
                        GreaterThanOrEqual = startDate.Value,
                        LessThanOrEqual = endDate ?? DateTime.UtcNow,
                    };
                }

                var invoices = await invoiceService.ListAsync(options);

                var result = invoices.Data.Select(invoice => new
                {
                    id = invoice.Id,
                    number = invoice.Number,
                    date = invoice.Created,
                    createdAt = invoice.Created,
                    amount = invoice.Total / 100.0m, // Convert from cents
                    subtotal = invoice.Subtotal / 100.0m,
                    tax = invoice.Tax / 100.0m,
                    total = invoice.Total / 100.0m,
                    status = invoice.Status,
                    description = invoice.Description ?? $"Subscription for {organization.Name}",
                    paymentMethod = invoice.PaymentIntent != null ? new
                    {
                        brand = invoice.Charge?.PaymentMethodDetails?.Card?.Brand,
                        last4 = invoice.Charge?.PaymentMethodDetails?.Card?.Last4,
                    } : null,
                    lineItems = invoice.Lines?.Data.Select(line => new
                    {
                        description = line.Description,
                        amount = line.Amount / 100.0m,
                        quantity = line.Quantity,
                        unitPrice = line.Price?.UnitAmount / 100.0m,
                    }).ToList(),
                }).ToList();

                // Calculate pagination
                var totalCount = invoices.Data.Count; // Note: Stripe pagination works differently
                var totalPages = (int)Math.Ceiling(totalCount / (double)limit);

                return Ok(new
                {
                    data = new
                    {
                        invoices = result,
                        total = totalCount,
                        pages = totalPages,
                    }
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error getting invoices");
                return BadRequest(new { message = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoices");
                return StatusCode(500, new { message = "Failed to get invoices" });
            }
        }

        /// <summary>
        /// Gets a single invoice by ID
        /// </summary>
        [HttpGet("{invoiceId}")]
        public async Task<IActionResult> GetInvoiceById(string invoiceId)
        {
            try
            {
                var invoiceService = new InvoiceService();
                var invoice = await invoiceService.GetAsync(invoiceId, new InvoiceGetOptions
                {
                    Expand = new List<string> { "payment_intent", "charge" },
                });

                var result = new
                {
                    id = invoice.Id,
                    number = invoice.Number,
                    date = invoice.Created,
                    createdAt = invoice.Created,
                    amount = invoice.Total / 100.0m,
                    subtotal = invoice.Subtotal / 100.0m,
                    tax = invoice.Tax / 100.0m,
                    discount = invoice.Discount?.Coupon?.AmountOff / 100.0m ?? 0m,
                    total = invoice.Total / 100.0m,
                    status = invoice.Status,
                    description = invoice.Description,
                    paymentMethod = invoice.Charge != null ? new
                    {
                        brand = invoice.Charge.PaymentMethodDetails?.Card?.Brand,
                        last4 = invoice.Charge.PaymentMethodDetails?.Card?.Last4,
                    } : null,
                    lineItems = invoice.Lines?.Data.Select(line => new
                    {
                        description = line.Description,
                        amount = line.Amount / 100.0m,
                        quantity = line.Quantity,
                        unitPrice = line.Price?.UnitAmount / 100.0m,
                    }).ToList(),
                    prorationDetails = invoice.Lines?.Data.Any(l => l.Proration == true) == true ? new
                    {
                        description = "This charges includes prorated amounts for mid-cycle changes",
                    } : null,
                };

                return Ok(new { data = result });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error getting invoice");
                return BadRequest(new { message = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoice");
                return StatusCode(500, new { message = "Failed to get invoice" });
            }
        }

        /// <summary>
        /// Downloads invoice as PDF
        /// </summary>
        [HttpGet("{invoiceId}/download")]
        public async Task<IActionResult> DownloadInvoice(string invoiceId)
        {
            try
            {
                var invoiceService = new InvoiceService();
                var invoice = await invoiceService.GetAsync(invoiceId);

                if (string.IsNullOrEmpty(invoice.InvoicePdf))
                {
                    return NotFound(new { message = "Invoice PDF not available" });
                }

                // Redirect to Stripe's hosted invoice PDF
                return Redirect(invoice.InvoicePdf);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error downloading invoice");
                return BadRequest(new { message = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading invoice");
                return StatusCode(500, new { message = "Failed to download invoice" });
            }
        }

        /// <summary>
        /// Gets upcoming invoice preview
        /// </summary>
        [HttpGet("upcoming/{subscriptionId}")]
        public async Task<IActionResult> GetUpcomingInvoice(string subscriptionId)
        {
            try
            {
                var invoiceService = new InvoiceService();
                var options = new UpcomingInvoiceOptions
                {
                    Subscription = subscriptionId,
                };

                var invoice = await invoiceService.UpcomingAsync(options);

                var result = new
                {
                    nextBillingDate = invoice.PeriodEnd,
                    amount = invoice.Total / 100.0m,
                    subtotal = invoice.Subtotal / 100.0m,
                    tax = invoice.Tax / 100.0m,
                    total = invoice.Total / 100.0m,
                    lineItems = invoice.Lines?.Data.Select(line => new
                    {
                        description = line.Description,
                        amount = line.Amount / 100.0m,
                        quantity = line.Quantity,
                    }).ToList(),
                };

                return Ok(new { data = result });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error getting upcoming invoice");
                return BadRequest(new { message = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming invoice");
                return StatusCode(500, new { message = "Failed to get upcoming invoice" });
            }
        }

        /// <summary>
        /// Gets billing summary for date range
        /// </summary>
        [HttpGet("organization/{organizationId}/summary")]
        public async Task<IActionResult> GetBillingSummary(
            Guid organizationId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
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
                    return Ok(new
                    {
                        data = new
                        {
                            totalPaid = 0m,
                            count = 0,
                            average = 0m,
                        }
                    });
                }

                var invoiceService = new InvoiceService();
                var options = new InvoiceListOptions
                {
                    Customer = organization.StripeCustomerId,
                    Status = "paid",
                    Created = new DateRangeOptions
                    {
                        GreaterThanOrEqual = startDate,
                        LessThanOrEqual = endDate,
                    },
                };

                var invoices = await invoiceService.ListAsync(options);

                var totalPaid = invoices.Data.Sum(i => i.Total) / 100.0m;
                var count = invoices.Data.Count;
                var average = count > 0 ? totalPaid / count : 0m;

                return Ok(new
                {
                    data = new
                    {
                        totalPaid,
                        count,
                        average,
                    }
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error getting billing summary");
                return BadRequest(new { message = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting billing summary");
                return StatusCode(500, new { message = "Failed to get billing summary" });
            }
        }
    }
}
