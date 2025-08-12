using System;
using System.Threading.Tasks;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Common.Interfaces
{
    /// <summary>
    /// Interface for Stripe payment processing.
    /// Handles customer creation, charge processing, subscription management, and webhook handling.
    /// </summary>
    public interface IStripePaymentService
    {
        /// <summary>
        /// Creates a Stripe customer for an organization.
        /// </summary>
        Task<string> CreateCustomerAsync(Organization organization, string email, string name);

        /// <summary>
        /// Creates a payment method token from payment details.
        /// </summary>
        Task<string> CreatePaymentMethodAsync(string cardToken);

        /// <summary>
        /// Charges a customer for a subscription.
        /// </summary>
        Task<(bool Success, string ChargeId, string ErrorMessage)> ChargeCustomerAsync(
            string customerId,
            decimal amount,
            string currency,
            string description,
            string paymentMethodId = null);

        /// <summary>
        /// Creates a charge with idempotency key to prevent duplicate charges.
        /// </summary>
        Task<(bool Success, string ChargeId, string ErrorMessage)> ChargeWithIdempotencyAsync(
            string customerId,
            decimal amount,
            string currency,
            string description,
            string idempotencyKey);

        /// <summary>
        /// Retrieves charge details from Stripe.
        /// </summary>
        Task<(bool Success, string Status, string ErrorMessage)> GetChargeStatusAsync(string chargeId);

        /// <summary>
        /// Refunds a charge partially or fully.
        /// </summary>
        Task<(bool Success, string RefundId, string ErrorMessage)> RefundChargeAsync(
            string chargeId,
            decimal? amount = null,
            string reason = null);

        /// <summary>
        /// Retrieves a customer's payment methods.
        /// </summary>
        Task<(bool Success, System.Collections.Generic.List<PaymentMethodDto> PaymentMethods, string ErrorMessage)> GetPaymentMethodsAsync(string customerId);

        /// <summary>
        /// Sets a default payment method for a customer.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> SetDefaultPaymentMethodAsync(
            string customerId,
            string paymentMethodId);

        /// <summary>
        /// Deletes a payment method.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> DeletePaymentMethodAsync(string paymentMethodId);

        /// <summary>
        /// Validates webhook signature from Stripe.
        /// </summary>
        bool ValidateWebhookSignature(string json, string signatureHeader);

        /// <summary>
        /// Handles webhook events from Stripe.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> HandleWebhookAsync(string json);

        // ==================== Stripe Subscriptions API ====================
        
        /// <summary>
        /// Creates a new Stripe subscription for a customer.
        /// </summary>
        /// <param name="customerId">Stripe customer ID</param>
        /// <param name="priceId">Stripe price ID</param>
        /// <param name="quantity">Number of licenses/seats</param>
        /// <param name="trialPeriodDays">Optional trial period in days</param>
        /// <returns>Subscription ID and subscription item ID</returns>
        Task<(bool Success, string SubscriptionId, string SubscriptionItemId, string ErrorMessage)> CreateSubscriptionAsync(
            string customerId,
            string priceId,
            int quantity,
            int trialPeriodDays = 0);

        /// <summary>
        /// Updates the quantity of a subscription.
        /// </summary>
        /// <param name="subscriptionId">Stripe subscription ID</param>
        /// <param name="subscriptionItemId">Stripe subscription item ID</param>
        /// <param name="newQuantity">New quantity of licenses</param>
        /// <returns>Success status and error message if any</returns>
        Task<(bool Success, string ErrorMessage)> UpdateSubscriptionQuantityAsync(
            string subscriptionId,
            string subscriptionItemId,
            int newQuantity);

        /// <summary>
        /// Cancels a Stripe subscription.
        /// </summary>
        /// <param name="subscriptionId">Stripe subscription ID</param>
        /// <param name="cancelImmediately">If true, cancel now. If false, cancel at period end</param>
        /// <returns>Success status and error message if any</returns>
        Task<(bool Success, string ErrorMessage)> CancelSubscriptionAsync(
            string subscriptionId,
            bool cancelImmediately = false);

        /// <summary>
        /// Gets subscription details from Stripe.
        /// </summary>
        /// <param name="subscriptionId">Stripe subscription ID</param>
        /// <returns>Subscription status and details</returns>
        Task<(bool Success, StripeSubscriptionDto Subscription, string ErrorMessage)> GetSubscriptionAsync(
            string subscriptionId);

        /// <summary>
        /// Reactivates a cancelled subscription (must be before period end).
        /// </summary>
        /// <param name="subscriptionId">Stripe subscription ID</param>
        /// <returns>Success status and error message if any</returns>
        Task<(bool Success, string ErrorMessage)> ReactivateSubscriptionAsync(string subscriptionId);
    }

    /// <summary>
    /// Payment method DTO for response.
    /// </summary>
    public class PaymentMethodDto
    {
        public string Id { get; set; }
        public string Type { get; set; } // card
        public string CardBrand { get; set; }
        public string CardLast4 { get; set; }
        public int CardExpiryMonth { get; set; }
        public int CardExpiryYear { get; set; }
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// Stripe subscription DTO for response.
    /// </summary>
    public class StripeSubscriptionDto
    {
        public string Id { get; set; }
        public string Status { get; set; } // active, canceled, past_due, etc.
        public string CustomerId { get; set; }
        public int Quantity { get; set; }
        public DateTime CurrentPeriodStart { get; set; }
        public DateTime CurrentPeriodEnd { get; set; }
        public DateTime? CancelAt { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
        public DateTime? TrialEnd { get; set; }
    }
}
