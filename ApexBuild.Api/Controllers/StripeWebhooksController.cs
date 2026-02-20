using System;
using System.IO;
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
    [AllowAnonymous] // Webhooks must be publicly accessible
    public class StripeWebhooksController : ControllerBase
    {
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StripeWebhooksController> _logger;

        public StripeWebhooksController(
            IStripePaymentService stripePaymentService,
            IUnitOfWork unitOfWork,
            ILogger<StripeWebhooksController> logger)
        {
            _stripePaymentService = stripePaymentService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Handles Stripe webhook events.
        /// Processes payment_intent.succeeded, charge.failed, customer.deleted, etc.
        /// </summary>
        [HttpPost("handle")]
        public async Task<IActionResult> HandleWebhook()
        {
            try
            {
                var json = await new StreamReader(Request.Body).ReadToEndAsync();
                var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

                // Validate webhook signature
                if (string.IsNullOrEmpty(signatureHeader) || !_stripePaymentService.ValidateWebhookSignature(json, signatureHeader))
                {
                    _logger.LogWarning("Invalid webhook signature received");
                    return Unauthorized();
                }

                var stripeEvent = EventUtility.ParseEvent(json);

                _logger.LogInformation(
                    "Processing Stripe webhook: {EventType}",
                    stripeEvent.Type);

                // Handle different event types
                switch (stripeEvent.Type)
                {
                    case "charge.succeeded":
                        await HandleChargeSucceeded(stripeEvent);
                        break;

                    case "charge.failed":
                        await HandleChargeFailed(stripeEvent);
                        break;

                    case "charge.refunded":
                        await HandleChargeRefunded(stripeEvent);
                        break;

                    case "customer.deleted":
                        await HandleCustomerDeleted(stripeEvent);
                        break;

                    case "payment_intent.succeeded":
                        await HandlePaymentIntentSucceeded(stripeEvent);
                        break;

                    case "payment_intent.payment_failed":
                        await HandlePaymentIntentFailed(stripeEvent);
                        break;

                    // Subscription Events
                    case "customer.subscription.created":
                        await HandleSubscriptionCreated(stripeEvent);
                        break;

                    case "customer.subscription.updated":
                        await HandleSubscriptionUpdated(stripeEvent);
                        break;

                    case "customer.subscription.deleted":
                        await HandleSubscriptionDeleted(stripeEvent);
                        break;

                    // Invoice Events
                    case "invoice.payment_succeeded":
                        await HandleInvoicePaymentSucceeded(stripeEvent);
                        break;

                    case "invoice.payment_failed":
                        await HandleInvoicePaymentFailed(stripeEvent);
                        break;

                    case "invoice.upcoming":
                        await HandleInvoiceUpcoming(stripeEvent);
                        break;

                    default:
                        _logger.LogInformation(
                            "Unhandled webhook event type: {EventType}",
                            stripeEvent.Type);
                        break;
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe webhook error: {Message}", ex.Message);
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook: {Message}", ex.Message);
                return StatusCode(500);
            }
        }

        private async Task HandleChargeSucceeded(Event stripeEvent)
        {
            var charge = stripeEvent.Data.Object as Charge;
            if (charge == null)
            {
                _logger.LogWarning("Failed to parse Charge object from webhook");
                return;
            }

            _logger.LogInformation(
                "Charge succeeded: {ChargeId}, Amount: {Amount}",
                charge.Id, charge.Amount);

            // Update payment transaction if it exists
            var transaction = await _unitOfWork.PaymentTransactions.GetByStripeChargeIdAsync(charge.Id);
            if (transaction != null)
            {
                transaction.Status = PaymentStatus.Completed;
                transaction.ProcessedAt = DateTime.UtcNow;
                _unitOfWork.PaymentTransactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Payment transaction {TransactionId} marked as completed for charge {ChargeId}",
                    transaction.Id, charge.Id);
            }
            else
            {
                _logger.LogWarning(
                    "No matching payment transaction found for successful charge {ChargeId}",
                    charge.Id);
            }
        }

        private async Task HandleChargeFailed(Event stripeEvent)
        {
            var charge = stripeEvent.Data.Object as Charge;
            if (charge == null)
            {
                _logger.LogWarning("Failed to parse Charge object from webhook");
                return;
            }

            _logger.LogWarning(
                "Charge failed: {ChargeId} - {FailureMessage}",
                charge.Id, charge.FailureMessage);

            // Update payment transaction
            var transaction = await _unitOfWork.PaymentTransactions.GetByStripeChargeIdAsync(charge.Id);
            if (transaction != null)
            {
                transaction.Status = PaymentStatus.Failed;
                transaction.ErrorMessage = charge.FailureMessage;
                _unitOfWork.PaymentTransactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogWarning(
                    "Payment transaction {TransactionId} marked as failed for charge {ChargeId}: {Reason}",
                    transaction.Id, charge.Id, charge.FailureMessage);
            }
        }

        private async Task HandleChargeRefunded(Event stripeEvent)
        {
            var charge = stripeEvent.Data.Object as Charge;
            if (charge == null)
            {
                _logger.LogWarning("Failed to parse Charge object from webhook");
                return;
            }

            _logger.LogInformation(
                "Charge refunded: {ChargeId}",
                charge.Id);

            // Update payment transaction
            var transaction = await _unitOfWork.PaymentTransactions.GetByStripeChargeIdAsync(charge.Id);
            if (transaction != null)
            {
                transaction.Status = charge.AmountRefunded == charge.Amount
                    ? PaymentStatus.Refunded
                    : PaymentStatus.PartiallyRefunded;
                transaction.RefundAmount = charge.AmountRefunded / 100m;
                transaction.RefundedAt = DateTime.UtcNow;
                _unitOfWork.PaymentTransactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private Task HandleCustomerDeleted(Event stripeEvent)
        {
            var customer = stripeEvent.Data.Object as Customer;
            if (customer == null)
            {
                _logger.LogWarning("Failed to parse Customer object from webhook");
                return Task.CompletedTask;
            }

            _logger.LogInformation(
                "Customer deleted: {CustomerId}",
                customer.Id);

            // Could handle cleanup of subscription/org associated with deleted customer
            return Task.CompletedTask;
        }

        private Task HandlePaymentIntentSucceeded(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null)
            {
                _logger.LogWarning("Failed to parse PaymentIntent object from webhook");
                return Task.CompletedTask;
            }

            _logger.LogInformation(
                "Payment intent succeeded: {PaymentIntentId}",
                paymentIntent.Id);

            // Handle payment intent success
            return Task.CompletedTask;
        }

        private Task HandlePaymentIntentFailed(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null)
            {
                _logger.LogWarning("Failed to parse PaymentIntent object from webhook");
                return Task.CompletedTask;
            }

            _logger.LogWarning(
                "Payment intent failed: {PaymentIntentId}",
                paymentIntent.Id);

            return Task.CompletedTask;
        }

        // ==================== Subscription Webhooks ====================

        private async Task HandleSubscriptionCreated(Event stripeEvent)
        {
            var subscription = stripeEvent.Data.Object as Subscription;
            if (subscription == null)
            {
                _logger.LogWarning("Failed to parse Subscription object from webhook");
                return;
            }

            _logger.LogInformation(
                "Subscription created: {SubscriptionId} for customer {CustomerId}",
                subscription.Id, subscription.CustomerId);

            // Find local subscription by Stripe ID and update if exists
            var localSubscription = await _unitOfWork.Subscriptions.GetByStripeSubscriptionIdAsync(subscription.Id);
            if (localSubscription != null)
            {
                localSubscription.Status = subscription.Status == "active" ? SubscriptionStatus.Active : SubscriptionStatus.PendingPayment;
                localSubscription.StripeCurrentPeriodStart = subscription.CurrentPeriodStart;
                localSubscription.StripeCurrentPeriodEnd = subscription.CurrentPeriodEnd;
                _unitOfWork.Subscriptions.Update(localSubscription);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private async Task HandleSubscriptionUpdated(Event stripeEvent)
        {
            var subscription = stripeEvent.Data.Object as Subscription;
            if (subscription == null)
            {
                _logger.LogWarning("Failed to parse Subscription object from webhook");
                return;
            }

            _logger.LogInformation(
                "Subscription updated: {SubscriptionId} status {Status}",
                subscription.Id, subscription.Status);

            var localSubscription = await _unitOfWork.Subscriptions.GetByStripeSubscriptionIdAsync(subscription.Id);
            if (localSubscription != null)
            {
                // Update status based on Stripe subscription status
                localSubscription.Status = subscription.Status switch
                {
                    "active" => SubscriptionStatus.Active,
                    "past_due" => SubscriptionStatus.PendingPayment,
                    "canceled" => SubscriptionStatus.Cancelled,
                    "unpaid" => SubscriptionStatus.PendingPayment,
                    "incomplete" => SubscriptionStatus.PendingPayment,
                    "incomplete_expired" => SubscriptionStatus.Expired,
                    "trialing" => SubscriptionStatus.Active,
                    _ => localSubscription.Status
                };

                localSubscription.StripeCurrentPeriodStart = subscription.CurrentPeriodStart;
                localSubscription.StripeCurrentPeriodEnd = subscription.CurrentPeriodEnd;
                localSubscription.BillingStartDate = subscription.CurrentPeriodStart;
                localSubscription.BillingEndDate = subscription.CurrentPeriodEnd;
                localSubscription.NextBillingDate = subscription.CurrentPeriodEnd;

                // Update quantity if changed
                if (subscription.Items?.Data != null && subscription.Items.Data.Count > 0)
                {
                    long? quantity = subscription.Items.Data[0].Quantity;

                    if (quantity != localSubscription.ActiveUserCount)
                    {
                        localSubscription.ActiveUserCount = (int)quantity;
                    }
                }

                _unitOfWork.Subscriptions.Update(localSubscription);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Local subscription {SubscriptionId} updated from Stripe webhook",
                    localSubscription.Id);
            }
        }

        private async Task HandleSubscriptionDeleted(Event stripeEvent)
        {
            var subscription = stripeEvent.Data.Object as Subscription;
            if (subscription == null)
            {
                _logger.LogWarning("Failed to parse Subscription object from webhook");
                return;
            }

            _logger.LogInformation(
                "Subscription deleted: {SubscriptionId}",
                subscription.Id);

            var localSubscription = await _unitOfWork.Subscriptions.GetByStripeSubscriptionIdAsync(subscription.Id);
            if (localSubscription != null)
            {
                localSubscription.Status = SubscriptionStatus.Cancelled;
                localSubscription.CancelledAt = DateTime.UtcNow;
                localSubscription.AutoRenew = false;
                _unitOfWork.Subscriptions.Update(localSubscription);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        // ==================== Invoice Webhooks ====================

        private async Task HandleInvoicePaymentSucceeded(Event stripeEvent)
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice == null)
            {
                _logger.LogWarning("Failed to parse Invoice object from webhook");
                return;
            }

            _logger.LogInformation(
                "Invoice payment succeeded: {InvoiceId} for subscription {SubscriptionId}",
                invoice.Id, invoice.SubscriptionId);

            if (!string.IsNullOrEmpty(invoice.SubscriptionId))
            {
                var localSubscription = await _unitOfWork.Subscriptions.GetByStripeSubscriptionIdAsync(invoice.SubscriptionId);
                if (localSubscription != null)
                {
                    // Mark subscription as active after successful payment
                    localSubscription.Status = SubscriptionStatus.Active;
                    localSubscription.BillingStartDate = invoice.PeriodStart;
                    localSubscription.BillingEndDate = invoice.PeriodEnd;
                    localSubscription.NextBillingDate = invoice.PeriodEnd;
                    localSubscription.StripeCurrentPeriodStart = invoice.PeriodStart;
                    localSubscription.StripeCurrentPeriodEnd = invoice.PeriodEnd;
                    
                    _unitOfWork.Subscriptions.Update(localSubscription);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation(
                        "Subscription {SubscriptionId} renewed via invoice payment",
                        localSubscription.Id);
                }
            }
        }

        private async Task HandleInvoicePaymentFailed(Event stripeEvent)
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice == null)
            {
                _logger.LogWarning("Failed to parse Invoice object from webhook");
                return;
            }

            _logger.LogWarning(
                "Invoice payment failed: {InvoiceId} for subscription {SubscriptionId}",
                invoice.Id, invoice.SubscriptionId);

            if (!string.IsNullOrEmpty(invoice.SubscriptionId))
            {
                var localSubscription = await _unitOfWork.Subscriptions.GetByStripeSubscriptionIdAsync(invoice.SubscriptionId);
                if (localSubscription != null)
                {
                    // Mark subscription as pending payment
                    localSubscription.Status = SubscriptionStatus.PendingPayment;
                    _unitOfWork.Subscriptions.Update(localSubscription);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogWarning(
                        "Subscription {SubscriptionId} marked as pending payment due to failed invoice",
                        localSubscription.Id);
                }
            }
        }

        private async Task HandleInvoiceUpcoming(Event stripeEvent)
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice == null)
            {
                _logger.LogWarning("Failed to parse Invoice object from webhook");
                return;
            }

            _logger.LogInformation(
                "Upcoming invoice: {InvoiceId} for subscription {SubscriptionId} on {Date}",
                invoice.Id, invoice.SubscriptionId, invoice.PeriodEnd);

            // TODO: Send notification to customer about upcoming payment
            // Could integrate with INotificationService or IEmailService here
        }
    }
}
