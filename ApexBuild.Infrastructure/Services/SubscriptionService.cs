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
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SubscriptionService> _logger;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly StripeSettings _stripeSettings;

        public SubscriptionService(
            IUnitOfWork unitOfWork,
            ILogger<SubscriptionService> logger,
            IStripePaymentService stripePaymentService,
            IOptions<StripeSettings> stripeSettings)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _stripePaymentService = stripePaymentService;
            _stripeSettings = stripeSettings.Value;
        }

        public async Task<(bool Success, Subscription Subscription, string ErrorMessage)> CreateSubscriptionAsync(
            Guid organizationId,
            Guid userId,
            int numberOfLicenses,
            int trialDays = 0)
        {
            try
            {
                // Check if subscription already exists
                var existingSubscription = await _unitOfWork.Subscriptions.GetByOrganizationIdAsync(organizationId);
                if (existingSubscription != null)
                {
                    return (false, null, "Organization already has an active subscription");
                }

                // Get organization to create Stripe customer if needed
                var organization = await _unitOfWork.Organizations.GetByIdAsync(organizationId);
                if (organization == null)
                {
                    return (false, null, "Organization not found");
                }

                // Get user for email
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return (false, null, "User not found");
                }

                // Create Stripe customer if doesn't exist
                string stripeCustomerId = organization.StripeCustomerId;
                if (string.IsNullOrEmpty(stripeCustomerId))
                {
                    stripeCustomerId = await _stripePaymentService.CreateCustomerAsync(
                        organization, user.Email, organization.Name);
                    organization.StripeCustomerId = stripeCustomerId;
                    _unitOfWork.Organizations.Update(organization);
                }

                // Create Stripe subscription
                var (stripeSuccess, stripeSubscriptionId, stripeSubscriptionItemId, stripeError) = 
                    await _stripePaymentService.CreateSubscriptionAsync(
                        stripeCustomerId,
                        _stripeSettings.MonthlyPriceId,
                        numberOfLicenses,
                        trialDays);

                if (!stripeSuccess)
                {
                    _logger.LogError("Failed to create Stripe subscription: {Error}", stripeError);
                    return (false, null, $"Failed to create subscription in Stripe: {stripeError}");
                }

                // Get subscription details from Stripe to use accurate dates
                var (getSuccess, stripeSubscriptionDto, getError) = 
                    await _stripePaymentService.GetSubscriptionAsync(stripeSubscriptionId);

                var subscription = new Subscription
                {
                    OrganizationId = organizationId,
                    UserId = userId,
                    NumberOfLicenses = numberOfLicenses,
                    LicensesUsed = 0,
                    Status = SubscriptionStatus.Active,
                    BillingStartDate = getSuccess ? stripeSubscriptionDto.CurrentPeriodStart : DateTime.UtcNow,
                    BillingEndDate = getSuccess ? stripeSubscriptionDto.CurrentPeriodEnd : DateTime.UtcNow.AddMonths(1),
                    NextBillingDate = getSuccess ? stripeSubscriptionDto.CurrentPeriodEnd : DateTime.UtcNow.AddMonths(1),
                    RenewalDate = getSuccess ? stripeSubscriptionDto.CurrentPeriodEnd : DateTime.UtcNow.AddMonths(1),
                    Amount = numberOfLicenses * _stripeSettings.MonthlyLicenseCost,
                    AutoRenew = true,
                    IsTrialPeriod = trialDays > 0,
                    TrialEndDate = getSuccess ? stripeSubscriptionDto.TrialEnd : (trialDays > 0 ? DateTime.UtcNow.AddDays(trialDays) : null),
                    StripeCustomerId = stripeCustomerId,
                    StripeSubscriptionId = stripeSubscriptionId,
                    StripeSubscriptionItemId = stripeSubscriptionItemId,
                    StripePriceId = _stripeSettings.MonthlyPriceId,
                    StripeCurrentPeriodStart = getSuccess ? stripeSubscriptionDto.CurrentPeriodStart : DateTime.UtcNow,
                    StripeCurrentPeriodEnd = getSuccess ? stripeSubscriptionDto.CurrentPeriodEnd : DateTime.UtcNow.AddMonths(1)
                };

                await _unitOfWork.Subscriptions.AddAsync(subscription);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Subscription created for organization {OrganizationId}: {SubscriptionId} with Stripe ID: {StripeSubscriptionId}",
                    organizationId, subscription.Id, stripeSubscriptionId);

                return (true, subscription, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription for organization {OrganizationId}", organizationId);
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool Success, OrganizationLicense License, string ErrorMessage)> AssignLicenseAsync(
            Guid organizationId,
            Guid userId)
        {
            try
            {
                // Check if user already has license
                var existingLicense = await _unitOfWork.OrganizationLicenses
                    .GetByUserAndOrganizationAsync(organizationId, userId);
                
                if (existingLicense != null && existingLicense.IsActive)
                {
                    return (false, null, "User already has an active license in this organization");
                }

                // Get subscription
                var subscription = await _unitOfWork.Subscriptions.GetByOrganizationIdAsync(organizationId);
                if (subscription == null)
                {
                    return (false, null, "Organization does not have an active subscription");
                }

                // Check available licenses
                if (subscription.RemainingLicenses <= 0)
                {
                    return (false, null, "No available licenses in this organization");
                }

                var validFrom = DateTime.UtcNow;
                var validUntil = subscription.BillingEndDate;

                var license = new OrganizationLicense
                {
                    OrganizationId = organizationId,
                    UserId = userId,
                    SubscriptionId = subscription.Id,
                    LicenseKey = GenerateLicenseKey(),
                    Status = LicenseStatus.Active,
                    AssignedAt = DateTime.UtcNow,
                    ValidFrom = validFrom,
                    ValidUntil = validUntil,
                    LicenseType = "Full"
                };

                await _unitOfWork.OrganizationLicenses.AddAsync(license);
                
                // Update subscription license count
                subscription.LicensesUsed += 1;
                _unitOfWork.Subscriptions.Update(subscription);
                
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "License assigned to user {UserId} in organization {OrganizationId}",
                    userId, organizationId);

                return (true, license, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error assigning license to user {UserId} in organization {OrganizationId}",
                    userId, organizationId);
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> RevokeLicenseAsync(
            Guid licenseId,
            string reason)
        {
            try
            {
                var license = await _unitOfWork.OrganizationLicenses.GetByIdAsync(licenseId);
                if (license == null)
                {
                    return (false, "License not found");
                }

                license.Status = LicenseStatus.Revoked;
                license.RevokedAt = DateTime.UtcNow;
                license.RevocationReason = reason;

                _unitOfWork.OrganizationLicenses.Update(license);

                // Update subscription license count
                var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(license.SubscriptionId);
                if (subscription != null)
                {
                    subscription.LicensesUsed = Math.Max(0, subscription.LicensesUsed - 1);
                    _unitOfWork.Subscriptions.Update(subscription);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("License {LicenseId} revoked: {Reason}", licenseId, reason);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking license {LicenseId}", licenseId);
                return (false, ex.Message);
            }
        }

        public async Task<(bool HasLicense, OrganizationLicense License)> HasActiveLicenseAsync(
            Guid organizationId,
            Guid userId)
        {
            try
            {
                var hasLicense = await _unitOfWork.OrganizationLicenses
                    .HasActiveLicenseAsync(organizationId, userId);

                if (hasLicense)
                {
                    var license = await _unitOfWork.OrganizationLicenses
                        .GetByUserAndOrganizationAsync(organizationId, userId);
                    return (true, license);
                }

                return (false, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error checking license for user {UserId} in organization {OrganizationId}",
                    userId, organizationId);
                return (false, null);
            }
        }

        public async Task<bool> ValidateLicenseAccessAsync(Guid organizationId, Guid userId)
        {
            var (hasLicense, _) = await HasActiveLicenseAsync(organizationId, userId);
            return hasLicense;
        }

        public async Task<List<OrganizationLicense>> GetOrganizationLicensesAsync(
            Guid organizationId,
            LicenseStatus? status = null)
        {
            try
            {
                return await _unitOfWork.OrganizationLicenses.GetByOrganizationIdAsync(organizationId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting licenses for organization {OrganizationId}", organizationId);
                return new List<OrganizationLicense>();
            }
        }

        public async Task<List<OrganizationLicense>> GetUserLicensesAsync(Guid userId)
        {
            try
            {
                return await _unitOfWork.OrganizationLicenses.GetByUserIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting licenses for user {UserId}", userId);
                return new List<OrganizationLicense>();
            }
        }

        public async Task<Subscription> GetSubscriptionAsync(Guid organizationId)
        {
            try
            {
                return await _unitOfWork.Subscriptions.GetByOrganizationIdAsync(organizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription for organization {OrganizationId}", organizationId);
                return null;
            }
        }

        public async Task<(bool Success, string ErrorMessage)> UpgradeSubscriptionAsync(
            Guid subscriptionId,
            int additionalLicenses)
        {
            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
                if (subscription == null)
                {
                    return (false, "Subscription not found");
                }

                var newQuantity = subscription.NumberOfLicenses + additionalLicenses;

                // Update in Stripe first
                if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId) && 
                    !string.IsNullOrEmpty(subscription.StripeSubscriptionItemId))
                {
                    var (stripeSuccess, stripeError) = await _stripePaymentService.UpdateSubscriptionQuantityAsync(
                        subscription.StripeSubscriptionId,
                        subscription.StripeSubscriptionItemId,
                        newQuantity);

                    if (!stripeSuccess)
                    {
                        _logger.LogError(
                            "Failed to update subscription {SubscriptionId} in Stripe: {Error}",
                            subscriptionId, stripeError);
                        return (false, $"Failed to update subscription in Stripe: {stripeError}");
                    }
                }

                // Update local database
                subscription.NumberOfLicenses = newQuantity;
                subscription.Amount = subscription.NumberOfLicenses * subscription.LicenseCostPerMonth;

                _unitOfWork.Subscriptions.Update(subscription);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Subscription {SubscriptionId} upgraded by {AdditionalLicenses} licenses to {NewQuantity}",
                    subscriptionId, additionalLicenses, newQuantity);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upgrading subscription {SubscriptionId}", subscriptionId);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> DowngradeSubscriptionAsync(
            Guid subscriptionId,
            int licensesToRemove)
        {
            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
                if (subscription == null)
                {
                    return (false, "Subscription not found");
                }

                var newQuantity = subscription.NumberOfLicenses - licensesToRemove;

                if (newQuantity < subscription.LicensesUsed)
                {
                    return (false, "Cannot remove licenses - would drop below currently used licenses");
                }

                // Update in Stripe first
                if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId) && 
                    !string.IsNullOrEmpty(subscription.StripeSubscriptionItemId))
                {
                    var (stripeSuccess, stripeError) = await _stripePaymentService.UpdateSubscriptionQuantityAsync(
                        subscription.StripeSubscriptionId,
                        subscription.StripeSubscriptionItemId,
                        newQuantity);

                    if (!stripeSuccess)
                    {
                        _logger.LogError(
                            "Failed to update subscription {SubscriptionId} in Stripe: {Error}",
                            subscriptionId, stripeError);
                        return (false, $"Failed to update subscription in Stripe: {stripeError}");
                    }
                }

                // Update local database
                subscription.NumberOfLicenses = newQuantity;
                subscription.Amount = subscription.NumberOfLicenses * subscription.LicenseCostPerMonth;

                _unitOfWork.Subscriptions.Update(subscription);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Subscription {SubscriptionId} downgraded by {LicensesToRemove} licenses to {NewQuantity}",
                    subscriptionId, licensesToRemove, newQuantity);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downgrading subscription {SubscriptionId}", subscriptionId);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> RenewSubscriptionAsync(Guid subscriptionId)
        {
            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
                if (subscription == null)
                {
                    return (false, "Subscription not found");
                }

                var newBillingStartDate = subscription.BillingEndDate;
                var newBillingEndDate = newBillingStartDate.AddMonths(1);

                subscription.BillingStartDate = newBillingStartDate;
                subscription.BillingEndDate = newBillingEndDate;
                subscription.NextBillingDate = newBillingEndDate;
                subscription.RenewalDate = newBillingEndDate;
                subscription.Status = SubscriptionStatus.Active;

                _unitOfWork.Subscriptions.Update(subscription);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Subscription {SubscriptionId} renewed", subscriptionId);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renewing subscription {SubscriptionId}", subscriptionId);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> CancelSubscriptionAsync(
            Guid subscriptionId,
            string reason)
        {
            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
                if (subscription == null)
                {
                    return (false, "Subscription not found");
                }

                // Cancel in Stripe first (at period end, not immediately)
                if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
                {
                    var (stripeSuccess, stripeError) = await _stripePaymentService.CancelSubscriptionAsync(
                        subscription.StripeSubscriptionId,
                        cancelImmediately: false); // Cancel at period end

                    if (!stripeSuccess)
                    {
                        _logger.LogError(
                            "Failed to cancel subscription {SubscriptionId} in Stripe: {Error}",
                            subscriptionId, stripeError);
                        return (false, $"Failed to cancel subscription in Stripe: {stripeError}");
                    }
                }

                // Update local database
                subscription.Status = SubscriptionStatus.Cancelled;
                subscription.CancelledAt = DateTime.UtcNow;
                subscription.CancellationReason = reason;
                subscription.AutoRenew = false;

                _unitOfWork.Subscriptions.Update(subscription);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Subscription {SubscriptionId} cancelled (at period end): {Reason}",
                    subscriptionId, reason);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
                return (false, ex.Message);
            }
        }

        public async Task<SubscriptionStatsDto> GetSubscriptionStatsAsync(Guid organizationId)
        {
            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetByOrganizationIdAsync(organizationId);
                if (subscription == null)
                {
                    return null;
                }

                var daysUntilRenewal = subscription.RenewalDate.HasValue 
                    ? (subscription.RenewalDate.Value - DateTime.UtcNow).Days 
                    : 0;

                return new SubscriptionStatsDto
                {
                    OrganizationId = organizationId,
                    TotalLicenses = subscription.NumberOfLicenses,
                    UsedLicenses = subscription.LicensesUsed,
                    AvailableLicenses = subscription.RemainingLicenses,
                    MonthlyAmount = subscription.TotalMonthlyAmount,
                    BillingStartDate = subscription.BillingStartDate,
                    BillingEndDate = subscription.BillingEndDate,
                    NextBillingDate = subscription.NextBillingDate,
                    Status = subscription.Status,
                    IsActive = subscription.IsActive,
                    DaysUntilRenewal = daysUntilRenewal,
                    IsExpiringSoon = daysUntilRenewal <= 7,
                    LicenseCostPerMonth = subscription.LicenseCostPerMonth
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription stats for organization {OrganizationId}", organizationId);
                return null;
            }
        }

        public async Task<List<Subscription>> GetExpiredSubscriptionsAsync()
        {
            try
            {
                return await _unitOfWork.Subscriptions.GetExpiredSubscriptionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expired subscriptions");
                return new List<Subscription>();
            }
        }

        public async Task<List<Subscription>> GetExpiringSubscriptionsAsync(int daysUntilExpiration = 7)
        {
            try
            {
                return await _unitOfWork.Subscriptions.GetExpiringSubscriptionsAsync(daysUntilExpiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring subscriptions");
                return new List<Subscription>();
            }
        }

        public async Task<List<(User User, OrganizationLicense License)>> GetOrganiztionActiveUsersWithLicensesAsync(
            Guid organizationId)
        {
            try
            {
                return await _unitOfWork.OrganizationLicenses
                    .GetOrganizationUsersWithLicensesAsync(organizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting organization users with licenses for organization {OrganizationId}",
                    organizationId);
                return new List<(User, OrganizationLicense)>();
            }
        }

        public async Task<(bool Success, string ErrorMessage)> ReactivateSubscriptionAsync(Guid subscriptionId)
        {
            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
                if (subscription == null)
                {
                    return (false, "Subscription not found");
                }

                if (subscription.Status != SubscriptionStatus.Cancelled)
                {
                    return (false, $"Only cancelled subscriptions can be reactivated. Current status: {subscription.Status}");
                }

                // Reactivate in Stripe first
                if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
                {
                    var (stripeSuccess, stripeError) = await _stripePaymentService.ReactivateSubscriptionAsync(
                        subscription.StripeSubscriptionId);

                    if (!stripeSuccess)
                    {
                        _logger.LogError(
                            "Failed to reactivate subscription {SubscriptionId} in Stripe: {Error}",
                            subscriptionId, stripeError);
                        return (false, $"Failed to reactivate subscription in Stripe: {stripeError}");
                    }
                }

                // Update local database
                subscription.Status = SubscriptionStatus.Active;
                subscription.AutoRenew = true;
                subscription.CancelledAt = null;
                subscription.CancellationReason = null;

                _unitOfWork.Subscriptions.Update(subscription);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Subscription {SubscriptionId} reactivated successfully",
                    subscriptionId);

                return (true, "Subscription reactivated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating subscription {SubscriptionId}", subscriptionId);
                return (false, $"Error reactivating subscription: {ex.Message}");
            }
        }

        private string GenerateLicenseKey()
        {
            return $"LIC-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }
    }
}
