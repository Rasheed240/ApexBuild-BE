using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Infrastructure.Configurations;

namespace ApexBuild.Infrastructure.Services
{
    /// <summary>
    /// Real Stripe payment service implementation using Stripe.NET SDK.
    /// Handles customer creation, payment processing, and Stripe Subscriptions API.
    /// </summary>
    public class StripePaymentService : IStripePaymentService
    {
        private readonly StripeSettings _stripeSettings;
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(
            IOptions<StripeSettings> stripeSettings,
            ILogger<StripePaymentService> logger)
        {
            _stripeSettings = stripeSettings.Value;
            _logger = logger;

            // Initialize Stripe API key
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        }

        #region Customer Management

        public async Task<string> CreateCustomerAsync(Organization organization, string email, string name)
        {
            try
            {
                var options = new CustomerCreateOptions
                {
                    Email = email,
                    Name = name,
                    Description = $"Organization: {organization.Name} (ID: {organization.Id})",
                    Metadata = new Dictionary<string, string>
                    {
                        { "OrganizationId", organization.Id.ToString() },
                        { "OrganizationName", organization.Name }
                    }
                };

                var service = new CustomerService();
                var customer = await service.CreateAsync(options);

                _logger.LogInformation(
                    "Stripe customer created for organization {OrganizationId}: {CustomerId}",
                    organization.Id, customer.Id);

                return customer.Id;
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating Stripe customer for organization {OrganizationId}: {ErrorMessage}",
                    organization.Id, ex.Message);
                throw;
            }
        }

        #endregion

        #region Payment Methods

        public async Task<string> CreatePaymentMethodAsync(string cardToken)
        {
            try
            {
                var options = new PaymentMethodCreateOptions
                {
                    Type = "card",
                    Card = new PaymentMethodCardOptions
                    {
                        Token = cardToken
                    }
                };

                var service = new PaymentMethodService();
                var paymentMethod = await service.CreateAsync(options);

                _logger.LogInformation("Payment method created: {PaymentMethodId}", paymentMethod.Id);

                return paymentMethod.Id;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error creating payment method: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<(bool Success, List<PaymentMethodDto> PaymentMethods, string ErrorMessage)> GetPaymentMethodsAsync(
            string customerId)
        {
            try
            {
                var options = new PaymentMethodListOptions
                {
                    Customer = customerId,
                    Type = "card"
                };

                var service = new PaymentMethodService();
                var paymentMethods = await service.ListAsync(options);

                var dtos = paymentMethods.Data.Select(pm => new PaymentMethodDto
                {
                    Id = pm.Id,
                    Type = pm.Type,
                    CardBrand = pm.Card?.Brand,
                    CardLast4 = pm.Card?.Last4,
                    CardExpiryMonth = (int)(pm.Card?.ExpMonth ?? 0),
                    CardExpiryYear = (int)(pm.Card?.ExpYear ?? 0),
                    IsDefault = false
                }).ToList();

                _logger.LogInformation(
                    "Retrieved {Count} payment methods for customer {CustomerId}",
                    dtos.Count, customerId);

                return (true, dtos, null);
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving payment methods for customer {CustomerId}: {ErrorMessage}",
                    customerId, ex.Message);
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> SetDefaultPaymentMethodAsync(
            string customerId,
            string paymentMethodId)
        {
            try
            {
                var options = new CustomerUpdateOptions
                {
                    InvoiceSettings = new CustomerInvoiceSettingsOptions
                    {
                        DefaultPaymentMethod = paymentMethodId
                    }
                };

                var service = new CustomerService();
                await service.UpdateAsync(customerId, options);

                _logger.LogInformation(
                    "Set default payment method for customer {CustomerId}: {PaymentMethodId}",
                    customerId, paymentMethodId);

                return (true, null);
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Error setting default payment method for customer {CustomerId}: {ErrorMessage}",
                    customerId, ex.Message);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> DeletePaymentMethodAsync(string paymentMethodId)
        {
            try
            {
                var service = new PaymentMethodService();
                await service.DetachAsync(paymentMethodId);

                _logger.LogInformation("Deleted payment method: {PaymentMethodId}", paymentMethodId);

                return (true, null);
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting payment method {PaymentMethodId}: {ErrorMessage}",
                    paymentMethodId, ex.Message);
                return (false, ex.Message);
            }
        }

        #endregion

        #region One-Time Charges (for non-subscription payments)

        public async Task<(bool Success, string ChargeId, string ErrorMessage)> ChargeCustomerAsync(
            string customerId,
            decimal amount,
            string currency,
            string description,
            string paymentMethodId = null)
        {
            try
            {
                var amountInCents = (long)(amount * 100);

                var options = new ChargeCreateOptions
                {
                    Amount = amountInCents,
                    Currency = currency.ToLower(),
                    Customer = customerId,
                    Description = description,
                    ReceiptEmail = null
                };

                if (!string.IsNullOrEmpty(paymentMethodId))
                {
                    options.Source = paymentMethodId;
                }

                var service = new ChargeService();
                var charge = await service.CreateAsync(options);

                if (charge.Paid)
                {
                    _logger.LogInformation(
                        "Charge created successfully for customer {CustomerId}: {ChargeId}",
                        customerId, charge.Id);
                    return (true, charge.Id, null);
                }
                else
                {
                    var errorMessage = charge.FailureMessage ?? "Charge was not successful";
                    _logger.LogWarning(
                        "Charge creation resulted in unpaid charge for customer {CustomerId}: {ErrorMessage}",
                        customerId, errorMessage);
                    return (false, charge.Id, errorMessage);
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Stripe error charging customer {CustomerId}: {ErrorMessage}",
                    customerId, ex.Message);
                return (false, null, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error charging customer {CustomerId}: {ErrorMessage}",
                    customerId, ex.Message);
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool Success, string ChargeId, string ErrorMessage)> ChargeWithIdempotencyAsync(
            string customerId,
            decimal amount,
            string currency,
            string description,
            string idempotencyKey)
        {
            try
            {
                var amountInCents = (long)(amount * 100);

                var options = new ChargeCreateOptions
                {
                    Amount = amountInCents,
                    Currency = currency.ToLower(),
                    Customer = customerId,
                    Description = description
                };

                var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };

                var service = new ChargeService();
                var charge = await service.CreateAsync(options, requestOptions);

                if (charge.Paid)
                {
                    _logger.LogInformation(
                        "Charge created with idempotency key for customer {CustomerId}: {ChargeId}",
                        customerId, charge.Id);
                    return (true, charge.Id, null);
                }
                else
                {
                    var errorMessage = charge.FailureMessage ?? "Charge was not successful";
                    return (false, charge.Id, errorMessage);
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Stripe error charging customer {CustomerId} with idempotency: {ErrorMessage}",
                    customerId, ex.Message);
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool Success, string Status, string ErrorMessage)> GetChargeStatusAsync(string chargeId)
        {
            try
            {
                var service = new ChargeService();
                var charge = await service.GetAsync(chargeId);

                _logger.LogInformation("Retrieved charge status for {ChargeId}: {Status}", chargeId, charge.Status);

                return (true, charge.Status, null);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error retrieving charge {ChargeId}: {ErrorMessage}", chargeId, ex.Message);
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool Success, string RefundId, string ErrorMessage)> RefundChargeAsync(
            string chargeId,
            decimal? amount = null,
            string reason = null)
        {
            try
            {
                var options = new RefundCreateOptions
                {
                    Charge = chargeId,
                    Reason = reason ?? "requested_by_customer"
                };

                if (amount.HasValue)
                {
                    options.Amount = (long)(amount.Value * 100);
                }

                var service = new RefundService();
                var refund = await service.CreateAsync(options);

                _logger.LogInformation(
                    "Refund created for charge {ChargeId}: {RefundId}",
                    chargeId, refund.Id);

                return (true, refund.Id, null);
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Error refunding charge {ChargeId}: {ErrorMessage}",
                    chargeId, ex.Message);
                return (false, null, ex.Message);
            }
        }

        #endregion

        #region Stripe Subscriptions API

        public async Task<(bool Success, string SubscriptionId, string SubscriptionItemId, string ErrorMessage)> CreateSubscriptionAsync(
            string customerId,
            string priceId,
            int quantity,
            int trialPeriodDays = 0)
        {
            try
            {
                var options = new SubscriptionCreateOptions
                {
                    Customer = customerId,
                    Items = new List<SubscriptionItemOptions>
                    {
                        new SubscriptionItemOptions
                        {
                            Price = priceId,
                            Quantity = quantity
                        }
                    },
                    PaymentBehavior = "default_incomplete",
                    Expand = new List<string> { "latest_invoice.payment_intent" }
                };

                if (trialPeriodDays > 0)
                {
                    options.TrialPeriodDays = trialPeriodDays;
                }

                var service = new Stripe.SubscriptionService();
                var subscription = await service.CreateAsync(options);

                var subscriptionItemId = subscription.Items.Data[0].Id;

                _logger.LogInformation(
                    "Stripe subscription created: {SubscriptionId} for customer {CustomerId}",
                    subscription.Id, customerId);

                return (true, subscription.Id, subscriptionItemId, null);
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating Stripe subscription for customer {CustomerId}: {ErrorMessage}",
                    customerId, ex.Message);
                return (false, null, null, ex.Message);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> UpdateSubscriptionQuantityAsync(
            string subscriptionId,
            string subscriptionItemId,
            int newQuantity)
        {
            try
            {
                var options = new SubscriptionItemUpdateOptions
                {
                    Quantity = newQuantity,
                    ProrationBehavior = "create_prorations" // Automatically handle prorated charges
                };

                var service = new SubscriptionItemService();
                var subscriptionItem = await service.UpdateAsync(subscriptionItemId, options);

                _logger.LogInformation(
                    "Updated subscription {SubscriptionId} quantity to {Quantity}",
                    subscriptionId, newQuantity);

                return (true, null);
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating subscription {SubscriptionId} quantity: {ErrorMessage}",
                    subscriptionId, ex.Message);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> CancelSubscriptionAsync(
            string subscriptionId,
            bool cancelImmediately = false)
        {
            try
            {
                var service = new Stripe.SubscriptionService();

                if (cancelImmediately)
                {
                    // Cancel immediately
                    await service.CancelAsync(subscriptionId);
                    _logger.LogInformation(
                        "Subscription {SubscriptionId} cancelled immediately",
                        subscriptionId);
                }
                else
                {
                    // Cancel at period end
                    var options = new SubscriptionUpdateOptions
                    {
                        CancelAtPeriodEnd = true
                    };
                    await service.UpdateAsync(subscriptionId, options);
                    _logger.LogInformation(
                        "Subscription {SubscriptionId} scheduled for cancellation at period end",
                        subscriptionId);
                }

                return (true, null);
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Error cancelling subscription {SubscriptionId}: {ErrorMessage}",
                    subscriptionId, ex.Message);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, StripeSubscriptionDto Subscription, string ErrorMessage)> GetSubscriptionAsync(
            string subscriptionId)
        {
            try
            {
                var service = new Stripe.SubscriptionService();
                var subscription = await service.GetAsync(subscriptionId);

                var dto = new StripeSubscriptionDto
                {
                    Id = subscription.Id,
                    Status = subscription.Status,
                    CustomerId = subscription.CustomerId,
                    Quantity = (int)(subscription.Items?.Data?.FirstOrDefault()?.Quantity ?? 0),
                    // Note: Stripe SDK v48 doesn't directly expose current period dates in simple properties
                    // These will be populated via webhook events
                    CurrentPeriodStart = DateTime.UtcNow, // placeholder
                    CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1), // placeholder
                    CancelAt = subscription.CancelAt,
                    CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
                    TrialEnd = subscription.TrialEnd
                };

                _logger.LogInformation(
                    "Retrieved subscription {SubscriptionId} with status {Status}",
                    subscriptionId, subscription.Status);

                return (true, dto, null);
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving subscription {SubscriptionId}: {ErrorMessage}",
                    subscriptionId, ex.Message);
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> ReactivateSubscriptionAsync(string subscriptionId)
        {
            try
            {
                var options = new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = false
                };

                var service = new Stripe.SubscriptionService();
                await service.UpdateAsync(subscriptionId, options);

                _logger.LogInformation(
                    "Subscription {SubscriptionId} reactivated",
                    subscriptionId);

                return (true, null);
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Error reactivating subscription {SubscriptionId}: {ErrorMessage}",
                    subscriptionId, ex.Message);
                return (false, ex.Message);
            }
        }

        #endregion

        #region Webhooks

        public bool ValidateWebhookSignature(string json, string signatureHeader)
        {
            try
            {
                EventUtility.ValidateSignature(json, signatureHeader, _stripeSettings.WebhookSecret);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Webhook signature validation failed: {ErrorMessage}", ex.Message);
                return false;
            }
        }

        public async Task<(bool Success, string ErrorMessage)> HandleWebhookAsync(string json)
        {
            try
            {
                var stripeEvent = EventUtility.ParseEvent(json);

                _logger.LogInformation(
                    "Webhook event received: {EventType} - {EventId}",
                    stripeEvent.Type, stripeEvent.Id);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling webhook: {ErrorMessage}", ex.Message);
                return (false, ex.Message);
            }
        }

        #endregion
    }
}
