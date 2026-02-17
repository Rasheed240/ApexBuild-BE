using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using ApexBuild.Infrastructure.Configurations;

namespace ApexBuild.Infrastructure.Services
{
    public class PaymentProcessingService : IPaymentProcessingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly StripeSettings _stripeSettings;
        private readonly ILogger<PaymentProcessingService> _logger;

        public PaymentProcessingService(
            IUnitOfWork unitOfWork,
            IStripePaymentService stripePaymentService,
            IBackgroundJobService backgroundJobService,
            IOptions<StripeSettings> stripeSettings,
            ILogger<PaymentProcessingService> logger)
        {
            _unitOfWork = unitOfWork;
            _stripePaymentService = stripePaymentService;
            _backgroundJobService = backgroundJobService;
            _stripeSettings = stripeSettings.Value;
            _logger = logger;
        }

        public async Task<(bool Success, string ChargeId, string ErrorMessage)> ChargeSubscriptionAsync(
            Guid subscriptionId,
            string stripeCustomerId)
        {
            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
                if (subscription == null)
                {
                    return (false, null, "Subscription not found");
                }

                if (string.IsNullOrEmpty(stripeCustomerId))
                {
                    return (false, null, "Stripe customer ID not configured");
                }

                var idempotencyKey = $"subscription-{subscriptionId}-{subscription.BillingEndDate:yyyyMMdd}";

                var chargeResult = await _stripePaymentService.ChargeWithIdempotencyAsync(
                    stripeCustomerId,
                    subscription.TotalMonthlyAmount,
                    _stripeSettings.Currency,
                    $"Subscription renewal - {subscription.Organization.Name}",
                    idempotencyKey);

                if (chargeResult.Success)
                {
                    // Create payment transaction record
                    var transaction = new PaymentTransaction
                    {
                        OrganizationId = subscription.OrganizationId,
                        SubscriptionId = subscriptionId,
                        UserId = subscription.UserId,
                        TransactionId = Guid.NewGuid().ToString(),
                        StripeChargeId = chargeResult.ChargeId,
                        PaymentType = PaymentType.Renewal,
                        PaymentMethod = PaymentMethod.CreditCard,
                        Status = PaymentStatus.Completed,
                        Amount = subscription.TotalMonthlyAmount,
                        TransactionDate = DateTime.UtcNow,
                        ProcessedAt = DateTime.UtcNow,
                        Description = $"Subscription renewal - {subscription.ActiveUserCount} active users",
                        RetryCount = 0
                    };

                    await _unitOfWork.PaymentTransactions.AddAsync(transaction);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation(
                        "Payment processed successfully for subscription {SubscriptionId}: {ChargeId}",
                        subscriptionId, chargeResult.ChargeId);

                    return (true, chargeResult.ChargeId, null);
                }
                else
                {
                    // Create failed payment transaction
                    var transaction = new PaymentTransaction
                    {
                        OrganizationId = subscription.OrganizationId,
                        SubscriptionId = subscriptionId,
                        UserId = subscription.UserId,
                        TransactionId = Guid.NewGuid().ToString(),
                        StripeChargeId = chargeResult.ChargeId,
                        PaymentType = PaymentType.Renewal,
                        PaymentMethod = PaymentMethod.CreditCard,
                        Status = PaymentStatus.Failed,
                        Amount = subscription.TotalMonthlyAmount,
                        TransactionDate = DateTime.UtcNow,
                        Description = $"Subscription renewal - {subscription.ActiveUserCount} active users",
                        ErrorMessage = chargeResult.ErrorMessage,
                        RetryCount = 0,
                        MaxRetries = _stripeSettings.MaxRetryAttempts,
                        NextRetryAt = DateTime.UtcNow.AddMinutes(_stripeSettings.RetryDelayMinutes)
                    };

                    await _unitOfWork.PaymentTransactions.AddAsync(transaction);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogWarning(
                        "Payment failed for subscription {SubscriptionId}: {ErrorMessage}",
                        subscriptionId, chargeResult.ErrorMessage);

                    // Schedule retry
                    _backgroundJobService.SchedulePaymentRetry(
                        transaction.Id,
                        transaction.NextRetryAt.Value);

                    return (false, null, chargeResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error charging subscription {SubscriptionId}",
                    subscriptionId);
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> RetryPaymentAsync(Guid paymentTransactionId)
        {
            try
            {
                var transaction = await _unitOfWork.PaymentTransactions.GetByIdAsync(paymentTransactionId);
                if (transaction == null)
                {
                    return (false, "Payment transaction not found");
                }

                if (transaction.Status == PaymentStatus.Completed)
                {
                    return (true, null); // Already completed
                }

                if (transaction.RetryCount >= transaction.MaxRetries)
                {
                    _logger.LogWarning(
                        "Payment retry exceeded max attempts for transaction {TransactionId}",
                        paymentTransactionId);
                    return (false, "Max retry attempts exceeded");
                }

                var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(transaction.SubscriptionId);
                if (subscription == null)
                {
                    return (false, "Subscription not found");
                }

                // Attempt charge
                var chargeResult = await _stripePaymentService.ChargeCustomerAsync(
                    subscription.StripeCustomerId,
                    transaction.Amount,
                    _stripeSettings.Currency,
                    $"Subscription renewal retry - {subscription.Organization.Name}");

                if (chargeResult.Success)
                {
                    transaction.Status = PaymentStatus.Completed;
                    transaction.ProcessedAt = DateTime.UtcNow;
                    transaction.StripeChargeId = chargeResult.ChargeId;
                    transaction.ErrorMessage = null;

                    _unitOfWork.PaymentTransactions.Update(transaction);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation(
                        "Payment retry succeeded for transaction {TransactionId}",
                        paymentTransactionId);

                    return (true, null);
                }
                else
                {
                    transaction.RetryCount += 1;
                    transaction.ErrorMessage = chargeResult.ErrorMessage;

                    if (transaction.RetryCount < transaction.MaxRetries)
                    {
                        transaction.NextRetryAt = DateTime.UtcNow.AddMinutes(
                            _stripeSettings.RetryDelayMinutes * transaction.RetryCount);
                        _backgroundJobService.SchedulePaymentRetry(
                            paymentTransactionId,
                            transaction.NextRetryAt.Value);
                    }
                    else
                    {
                        transaction.Status = PaymentStatus.Failed;
                    }

                    _unitOfWork.PaymentTransactions.Update(transaction);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogWarning(
                        "Payment retry attempt {RetryCount} failed for transaction {TransactionId}: {ErrorMessage}",
                        transaction.RetryCount, paymentTransactionId, chargeResult.ErrorMessage);

                    return (false, chargeResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrying payment for transaction {PaymentTransactionId}",
                    paymentTransactionId);
                return (false, ex.Message);
            }
        }

        public async Task ProcessFailedPaymentsAsync()
        {
            try
            {
                _logger.LogInformation("Starting hourly failed payment retry check");

                var retryablePayments = await _unitOfWork.PaymentTransactions.GetRetryablePaymentsAsync();

                _logger.LogInformation("Found {Count} payments eligible for retry", retryablePayments.Count);

                foreach (var payment in retryablePayments)
                {
                    await RetryPaymentAsync(payment.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing failed payments");
            }
        }

        public async Task<(bool Success, string RefundId, string ErrorMessage)> RefundPaymentAsync(
            Guid paymentTransactionId,
            decimal? amount = null,
            string reason = null)
        {
            try
            {
                var transaction = await _unitOfWork.PaymentTransactions.GetByIdAsync(paymentTransactionId);
                if (transaction == null)
                {
                    return (false, null, "Payment transaction not found");
                }

                if (string.IsNullOrEmpty(transaction.StripeChargeId))
                {
                    return (false, null, "No Stripe charge ID available for refund");
                }

                var refundResult = await _stripePaymentService.RefundChargeAsync(
                    transaction.StripeChargeId,
                    amount,
                    reason ?? "requested_by_customer");

                if (refundResult.Success)
                {
                    transaction.RefundedAt = DateTime.UtcNow;
                    transaction.RefundAmount = amount ?? transaction.TotalAmount;
                    transaction.RefundReason = reason;
                    transaction.Status = amount.HasValue && amount < transaction.TotalAmount
                        ? PaymentStatus.PartiallyRefunded
                        : PaymentStatus.Refunded;

                    _unitOfWork.PaymentTransactions.Update(transaction);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation(
                        "Refund processed for transaction {TransactionId}: {RefundId}",
                        paymentTransactionId, refundResult.RefundId);

                    return (true, refundResult.RefundId, null);
                }
                else
                {
                    _logger.LogWarning(
                        "Refund failed for transaction {TransactionId}: {ErrorMessage}",
                        paymentTransactionId, refundResult.ErrorMessage);
                    return (false, null, refundResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error refunding payment {PaymentTransactionId}",
                    paymentTransactionId);
                return (false, null, ex.Message);
            }
        }
    }
}
