using System;
using System.Threading.Tasks;

namespace ApexBuild.Application.Common.Interfaces
{
    /// <summary>
    /// Interface for subscription billing operations.
    /// Handles billing cycles, renewals, and subscription lifecycle.
    /// </summary>
    public interface ISubscriptionBillingService
    {
        /// <summary>
        /// Processes subscription renewal for a specific subscription.
        /// </summary>
        Task ProcessSubscriptionRenewalAsync(Guid subscriptionId);

        /// <summary>
        /// Processes all expiring subscriptions.
        /// Called by background job daily.
        /// </summary>
        Task ProcessExpiringSubscriptionsAsync();

        /// <summary>
        /// Sends renewal notification to organization owner.
        /// </summary>
        Task SendRenewalNotificationAsync(Guid subscriptionId);

        /// <summary>
        /// Handles subscription expiration.
        /// Marks as expired and revokes all licenses.
        /// </summary>
        Task HandleSubscriptionExpirationAsync(Guid subscriptionId);
    }

    /// <summary>
    /// Interface for payment processing operations.
    /// Handles charging, refunds, and payment retry logic.
    /// </summary>
    public interface IPaymentProcessingService
    {
        /// <summary>
        /// Charges an organization for their subscription.
        /// </summary>
        Task<(bool Success, string ChargeId, string ErrorMessage)> ChargeSubscriptionAsync(
            Guid subscriptionId,
            string stripeCustomerId);

        /// <summary>
        /// Retries a failed payment transaction.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> RetryPaymentAsync(Guid paymentTransactionId);

        /// <summary>
        /// Processes all failed payments that are eligible for retry.
        /// Called by background job hourly.
        /// </summary>
        Task ProcessFailedPaymentsAsync();

        /// <summary>
        /// Refunds a payment transaction.
        /// </summary>
        Task<(bool Success, string RefundId, string ErrorMessage)> RefundPaymentAsync(
            Guid paymentTransactionId,
            decimal? amount = null,
            string reason = null);
    }

    /// <summary>
    /// Interface for license notification operations.
    /// Handles notifications about license status.
    /// </summary>
    public interface ILicenseNotificationService
    {
        /// <summary>
        /// Sends license expiration notification for a specific license.
        /// </summary>
        Task SendLicenseExpirationNotificationAsync(Guid licenseId);

        /// <summary>
        /// Checks and sends notifications for licenses expiring soon.
        /// Called by background job daily.
        /// </summary>
        Task CheckAndNotifyExpiringLicensesAsync();

        /// <summary>
        /// Sends license assigned notification.
        /// </summary>
        Task SendLicenseAssignedNotificationAsync(Guid licenseId);

        /// <summary>
        /// Sends license revoked notification.
        /// </summary>
        Task SendLicenseRevokedNotificationAsync(Guid licenseId);
    }
}
