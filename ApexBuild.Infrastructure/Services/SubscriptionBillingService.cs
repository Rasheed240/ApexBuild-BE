using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Infrastructure.Services
{
    public class SubscriptionBillingService : ISubscriptionBillingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentProcessingService _paymentProcessingService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<SubscriptionBillingService> _logger;

        public SubscriptionBillingService(
            IUnitOfWork unitOfWork,
            IPaymentProcessingService paymentProcessingService,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<SubscriptionBillingService> logger)
        {
            _unitOfWork = unitOfWork;
            _paymentProcessingService = paymentProcessingService;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task ProcessSubscriptionRenewalAsync(Guid subscriptionId)
        {
            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
                if (subscription == null)
                {
                    _logger.LogWarning("Subscription {SubscriptionId} not found", subscriptionId);
                    return;
                }

                if (!subscription.AutoRenew || subscription.Status == SubscriptionStatus.Cancelled)
                {
                    _logger.LogInformation("Subscription {SubscriptionId} is not eligible for renewal", subscriptionId);
                    return;
                }

                var chargeResult = await _paymentProcessingService.ChargeSubscriptionAsync(subscriptionId, subscription.StripeCustomerId);

                if (chargeResult.Success)
                {
                    subscription.BillingStartDate = subscription.BillingEndDate;
                    subscription.BillingEndDate = subscription.BillingEndDate.AddMonths(1);
                    subscription.NextBillingDate = subscription.BillingEndDate;
                    subscription.RenewalDate = subscription.BillingEndDate;
                    subscription.Status = SubscriptionStatus.Active;

                    _unitOfWork.Subscriptions.Update(subscription);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Subscription {SubscriptionId} renewed successfully. Charge ID: {ChargeId}", subscriptionId, chargeResult.ChargeId);

                    await SendRenewalNotificationAsync(subscriptionId);
                }
                else
                {
                    _logger.LogWarning("Failed to charge subscription {SubscriptionId}: {ErrorMessage}", subscriptionId, chargeResult.ErrorMessage);
                    subscription.Status = SubscriptionStatus.PendingPayment;
                    _unitOfWork.Subscriptions.Update(subscription);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscription renewal for {SubscriptionId}", subscriptionId);
            }
        }

        public async Task ProcessExpiringSubscriptionsAsync()
        {
            try
            {
                _logger.LogInformation("Starting daily subscription renewal check");

                var expiringSubscriptions = await _unitOfWork.Subscriptions.GetExpiringSubscriptionsAsync(1);

                _logger.LogInformation("Found {Count} subscriptions expiring within 24 hours", expiringSubscriptions.Count);

                foreach (var subscription in expiringSubscriptions.Where(s => s.AutoRenew))
                {
                    await ProcessSubscriptionRenewalAsync(subscription.Id);
                }

                var expiredSubscriptions = await _unitOfWork.Subscriptions.GetExpiredSubscriptionsAsync();
                foreach (var subscription in expiredSubscriptions
                    .Where(s => s.Status != SubscriptionStatus.Expired && s.Status != SubscriptionStatus.Cancelled))
                {
                    await HandleSubscriptionExpirationAsync(subscription.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expiring subscriptions");
            }
        }

        public async Task SendRenewalNotificationAsync(Guid subscriptionId)
        {
            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
                if (subscription == null) return;

                var owner = subscription.User;
                if (owner == null) return;

                var subject = "Subscription Renewal Confirmation - " + subscription.Organization.Name;
                var message = "Your subscription for " + subscription.Organization.Name + " has been successfully renewed.\n\n"
                    + "Active Users: " + subscription.ActiveUserCount + "\n"
                    + "Monthly Amount: " + subscription.TotalMonthlyAmount.ToString("F2") + "\n"
                    + "Billing Period: " + subscription.BillingStartDate.ToString("yyyy-MM-dd") + " to " + subscription.BillingEndDate.ToString("yyyy-MM-dd") + "\n"
                    + "Next Renewal: " + subscription.RenewalDate?.ToString("yyyy-MM-dd") + "\n";

                await _emailService.SendEmailAsync(owner.Email, subject, message);

                _logger.LogInformation("Renewal notification sent to {Email} for subscription {SubscriptionId}", owner.Email, subscriptionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending renewal notification for subscription {SubscriptionId}", subscriptionId);
            }
        }

        public async Task HandleSubscriptionExpirationAsync(Guid subscriptionId)
        {
            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
                if (subscription == null) return;

                _logger.LogInformation("Handling expiration for subscription {SubscriptionId}", subscriptionId);

                subscription.Status = SubscriptionStatus.Expired;
                subscription.AutoRenew = false;
                _unitOfWork.Subscriptions.Update(subscription);

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Subscription {SubscriptionId} expired.", subscriptionId);

                var owner = subscription.User;
                if (owner != null)
                {
                    var subject = "Subscription Expired - " + subscription.Organization.Name;
                    var message = "Your subscription for " + subscription.Organization.Name + " has expired.\n\n"
                        + "Users will no longer have access to the platform.\n\n"
                        + "Please contact support or renew your subscription to regain access.\n";

                    await _emailService.SendEmailAsync(owner.Email, subject, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling subscription expiration for {SubscriptionId}", subscriptionId);
            }
        }
    }
}
