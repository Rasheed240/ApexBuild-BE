using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public PaymentsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get payment history for an organization.
        /// </summary>
        [HttpGet("organization/{organizationId}")]
        public async Task<ActionResult<List<PaymentTransactionDto>>> GetOrganizationPaymentHistory(Guid organizationId)
        {
            var transactions = await _unitOfWork.PaymentTransactions.GetByOrganizationIdAsync(organizationId);

            var dtos = transactions.Select(t => new PaymentTransactionDto
            {
                Id = t.Id,
                OrganizationId = t.OrganizationId,
                TransactionId = t.TransactionId,
                Amount = t.Amount,
                TotalAmount = t.TotalAmount,
                Status = t.Status.ToString(),
                PaymentType = t.PaymentType.ToString(),
                TransactionDate = t.TransactionDate,
                ProcessedAt = t.ProcessedAt,
                Description = t.Description,
                CardLast4 = t.CardLast4,
                CardBrand = t.CardBrand,
                InvoiceUrl = t.InvoiceUrl
            }).ToList();

            return Ok(dtos);
        }

        /// <summary>
        /// Get payment history for a subscription.
        /// </summary>
        [HttpGet("subscription/{subscriptionId}")]
        public async Task<ActionResult<List<PaymentTransactionDto>>> GetSubscriptionPaymentHistory(Guid subscriptionId)
        {
            var transactions = await _unitOfWork.PaymentTransactions.GetBySubscriptionIdAsync(subscriptionId);

            var dtos = transactions.Select(t => new PaymentTransactionDto
            {
                Id = t.Id,
                SubscriptionId = t.SubscriptionId,
                TransactionId = t.TransactionId,
                Amount = t.Amount,
                TotalAmount = t.TotalAmount,
                Status = t.Status.ToString(),
                PaymentType = t.PaymentType.ToString(),
                TransactionDate = t.TransactionDate,
                ProcessedAt = t.ProcessedAt,
                Description = t.Description,
                CardLast4 = t.CardLast4,
                CardBrand = t.CardBrand,
                InvoiceUrl = t.InvoiceUrl
            }).ToList();

            return Ok(dtos);
        }

        /// <summary>
        /// Get revenue statistics for a period.
        /// </summary>
        [HttpGet("revenue")]
        public async Task<ActionResult<RevenueStatsDto>> GetRevenueStats(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            if (endDate <= startDate)
            {
                return BadRequest("End date must be after start date");
            }

            var totalRevenue = await _unitOfWork.PaymentTransactions
                .GetTotalRevenueForPeriodAsync(startDate, endDate);

            var failedPayments = await _unitOfWork.PaymentTransactions
                .GetByStatusAsync(PaymentStatus.Failed);

            var completedPayments = await _unitOfWork.PaymentTransactions
                .GetByStatusAsync(PaymentStatus.Completed);

            return Ok(new RevenueStatsDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalRevenue = totalRevenue,
                TotalTransactions = completedPayments.Count,
                FailedTransactions = failedPayments.Count,
                AverageTransactionAmount = completedPayments.Count > 0
                    ? completedPayments.Sum(p => p.TotalAmount) / completedPayments.Count
                    : 0
            });
        }

        /// <summary>
        /// Get payment details.
        /// </summary>
        [HttpGet("{paymentId}")]
        public async Task<ActionResult<PaymentTransactionDto>> GetPaymentDetails(Guid paymentId)
        {
            var transaction = await _unitOfWork.PaymentTransactions.GetByIdAsync(paymentId);
            if (transaction == null)
            {
                return NotFound();
            }

            var dto = new PaymentTransactionDto
            {
                Id = transaction.Id,
                OrganizationId = transaction.OrganizationId,
                SubscriptionId = transaction.SubscriptionId,
                TransactionId = transaction.TransactionId,
                Amount = transaction.Amount,
                TaxAmount = transaction.TaxAmount,
                DiscountAmount = transaction.DiscountAmount,
                TotalAmount = transaction.TotalAmount,
                Status = transaction.Status.ToString(),
                PaymentType = transaction.PaymentType.ToString(),
                TransactionDate = transaction.TransactionDate,
                ProcessedAt = transaction.ProcessedAt,
                RefundedAt = transaction.RefundedAt,
                Description = transaction.Description,
                CardLast4 = transaction.CardLast4,
                CardBrand = transaction.CardBrand,
                CardExpiryMonth = transaction.CardExpiryMonth,
                CardExpiryYear = transaction.CardExpiryYear,
                InvoiceUrl = transaction.InvoiceUrl,
                ReceiptUrl = transaction.ReceiptUrl,
                ErrorMessage = transaction.ErrorMessage
            };

            return Ok(dto);
        }
    }

    public class PaymentTransactionDto
    {
        public Guid Id { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? SubscriptionId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? RefundedAt { get; set; }
        public string? Description { get; set; }
        public string? CardLast4 { get; set; }
        public string? CardBrand { get; set; }
        public int? CardExpiryMonth { get; set; }
        public int? CardExpiryYear { get; set; }
        public string? InvoiceUrl { get; set; }
        public string? ReceiptUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class RevenueStatsDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public decimal AverageTransactionAmount { get; set; }
    }
}
