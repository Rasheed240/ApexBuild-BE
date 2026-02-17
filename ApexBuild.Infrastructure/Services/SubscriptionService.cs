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

        public SubscriptionService(IUnitOfWork unitOfWork, ILogger<SubscriptionService> logger, IStripePaymentService stripePaymentService, IOptions<StripeSettings> stripeSettings)
        {
            _unitOfWork = unitOfWork; _logger = logger;
            _stripePaymentService = stripePaymentService; _stripeSettings = stripeSettings.Value;
        }

        public async Task<(bool Success, Subscription Subscription, string ErrorMessage)> CreateSubscriptionAsync(Guid organizationId, Guid userId, bool isFreePlan = false, int trialDays = 0)
        {
            try
            {
                var existing = await _unitOfWork.Subscriptions.GetByOrganizationIdAsync(organizationId);
                if (existing != null) return (false, null!, "Organization already has an active subscription");
                var organization = await _unitOfWork.Organizations.GetByIdAsync(organizationId);
                if (organization == null) return (false, null!, "Organization not found");
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null) return (false, null!, "User not found");
                string? stripeCustomerId = null; string? stripeSubscriptionId = null; string? stripeSubscriptionItemId = null;
                DateTime billingStart = DateTime.UtcNow;
                DateTime billingEnd = trialDays > 0 ? DateTime.UtcNow.AddDays(trialDays) : DateTime.UtcNow.AddMonths(1);
                if (!isFreePlan)
                {
                    stripeCustomerId = organization.StripeCustomerId;
                    if (string.IsNullOrEmpty(stripeCustomerId))
                    {
                        stripeCustomerId = await _stripePaymentService.CreateCustomerAsync(organization, user.Email, organization.Name);
                        organization.StripeCustomerId = stripeCustomerId;
                        _unitOfWork.Organizations.Update(organization);
                    }
                    var (ss, subId, subItemId, se) = await _stripePaymentService.CreateSubscriptionAsync(stripeCustomerId, _stripeSettings.MonthlyPriceId, 1, trialDays);
                    if (!ss) { _logger.LogError("Failed to create Stripe subscription: {Error}", se); return (false, null!, $"Failed to create subscription: {se}"); }
                    stripeSubscriptionId = subId; stripeSubscriptionItemId = subItemId;
                    var (gs, sDto, _) = await _stripePaymentService.GetSubscriptionAsync(stripeSubscriptionId);
                    if (gs && sDto != null) { billingStart = sDto.CurrentPeriodStart; billingEnd = sDto.CurrentPeriodEnd; }
                }
                var subscription = new Subscription { OrganizationId = organizationId, UserId = userId, IsFreePlan = isFreePlan, ActiveUserCount = 0, UserMonthlyRate = isFreePlan ? 0m : 20m, Status = SubscriptionStatus.Active, BillingStartDate = billingStart, BillingEndDate = billingEnd, NextBillingDate = billingEnd, RenewalDate = billingEnd, Amount = 0m, AutoRenew = !isFreePlan, IsTrialPeriod = trialDays > 0, TrialEndDate = trialDays > 0 ? DateTime.UtcNow.AddDays(trialDays) : (DateTime?)null, StripeCustomerId = stripeCustomerId, StripeSubscriptionId = stripeSubscriptionId, StripeSubscriptionItemId = stripeSubscriptionItemId, StripePriceId = isFreePlan ? null : _stripeSettings.MonthlyPriceId, StripeCurrentPeriodStart = isFreePlan ? (DateTime?)null : billingStart, StripeCurrentPeriodEnd = isFreePlan ? (DateTime?)null : billingEnd };
                await _unitOfWork.Subscriptions.AddAsync(subscription); await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Subscription created for org {OrganizationId}: {SubscriptionId}", organizationId, subscription.Id);
                return (true, subscription, null!);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error creating subscription"); return (false, null!, ex.Message); }
        }

        public async Task<(bool Success, string ErrorMessage)> CancelSubscriptionAsync(Guid subscriptionId, string reason)
        {
            try
            {
                var sub = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
                if (sub == null) return (false, "Subscription not found");
                if (!string.IsNullOrEmpty(sub.StripeSubscriptionId)) { var (ok, err) = await _stripePaymentService.CancelSubscriptionAsync(sub.StripeSubscriptionId, cancelImmediately: false); if (!ok) return (false, $"Failed to cancel in Stripe: {err}"); }
                sub.Status = SubscriptionStatus.Cancelled; sub.CancelledAt = DateTime.UtcNow; sub.CancellationReason = reason; sub.AutoRenew = false;
                _unitOfWork.Subscriptions.Update(sub); await _unitOfWork.SaveChangesAsync(); return (true, null!);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool Success, string ErrorMessage)> RenewSubscriptionAsync(Guid subscriptionId)
        {
            try
            {
                var sub = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
                if (sub == null) return (false, "Subscription not found");
                var ns = sub.BillingEndDate; var ne = ns.AddMonths(1);
                sub.BillingStartDate = ns; sub.BillingEndDate = ne; sub.NextBillingDate = ne; sub.RenewalDate = ne; sub.Status = SubscriptionStatus.Active;
                _unitOfWork.Subscriptions.Update(sub); await _unitOfWork.SaveChangesAsync(); return (true, null!);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool Success, string ErrorMessage)> ReactivateSubscriptionAsync(Guid subscriptionId)
        {
            try
            {
                var sub = await _unitOfWork.Subscriptions.GetByIdAsync(subscriptionId);
                if (sub == null) return (false, "Subscription not found");
                if (sub.Status != SubscriptionStatus.Cancelled) return (false, $"Cannot reactivate: status is {sub.Status}");
                if (!string.IsNullOrEmpty(sub.StripeSubscriptionId)) { var (ok, err) = await _stripePaymentService.ReactivateSubscriptionAsync(sub.StripeSubscriptionId); if (!ok) return (false, $"Failed to reactivate: {err}"); }
                sub.Status = SubscriptionStatus.Active; sub.AutoRenew = true; sub.CancelledAt = null; sub.CancellationReason = null;
                _unitOfWork.Subscriptions.Update(sub); await _unitOfWork.SaveChangesAsync(); return (true, "Subscription reactivated successfully");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<Subscription?> GetSubscriptionAsync(Guid organizationId)
        {
            try { return await _unitOfWork.Subscriptions.GetByOrganizationIdAsync(organizationId); }
            catch (Exception ex) { _logger.LogError(ex, "Error getting subscription"); return null; }
        }

        public async Task<SubscriptionStatsDto> GetSubscriptionStatsAsync(Guid organizationId)
        {
            try
            {
                var sub = await _unitOfWork.Subscriptions.GetByOrganizationIdAsync(organizationId);
                if (sub == null) return null!;
                var days = sub.RenewalDate.HasValue ? (sub.RenewalDate.Value - DateTime.UtcNow).Days : 0;
                return new SubscriptionStatsDto { OrganizationId = organizationId, ActiveUserCount = sub.ActiveUserCount, UserMonthlyRate = sub.UserMonthlyRate, TotalMonthlyAmount = sub.TotalMonthlyAmount, IsFreePlan = sub.IsFreePlan, BillingStartDate = sub.BillingStartDate, BillingEndDate = sub.BillingEndDate, NextBillingDate = sub.NextBillingDate, Status = sub.Status, IsActive = sub.IsActive, DaysUntilRenewal = days, IsExpiringSoon = days <= 7 };
            }
            catch (Exception ex) { _logger.LogError(ex, "Error getting stats"); return null!; }
        }

        public async Task UpdateActiveUserCountAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var sub = await _unitOfWork.Subscriptions.GetByOrganizationIdAsync(organizationId, cancellationToken);
                if (sub == null || sub.IsFreePlan) return;
                var members = await _unitOfWork.OrganizationMembers.FindAsync(om => om.OrganizationId == organizationId && om.IsActive, cancellationToken);
                var count = members.Count(); sub.ActiveUserCount = count; sub.Amount = sub.TotalMonthlyAmount;
                if (!string.IsNullOrEmpty(sub.StripeSubscriptionId) && !string.IsNullOrEmpty(sub.StripeSubscriptionItemId) && count > 0)
                {
                    var (ok, err) = await _stripePaymentService.UpdateSubscriptionQuantityAsync(sub.StripeSubscriptionId, sub.StripeSubscriptionItemId, count);
                    if (!ok) _logger.LogWarning("Failed to sync user count to Stripe: {Error}", err);
                }
                _unitOfWork.Subscriptions.Update(sub); await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error updating active user count"); }
        }

        public async Task<List<Subscription>> GetExpiringSubscriptionsAsync(int daysUntilExpiration = 7)
        {
            try { return await _unitOfWork.Subscriptions.GetExpiringSubscriptionsAsync(daysUntilExpiration); }
            catch (Exception ex) { _logger.LogError(ex, "Error getting expiring subscriptions"); return new List<Subscription>(); }
        }

        public async Task<List<Subscription>> GetExpiredSubscriptionsAsync()
        {
            try { return await _unitOfWork.Subscriptions.GetExpiredSubscriptionsAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Error getting expired subscriptions"); return new List<Subscription>(); }
        }

        public async Task<bool> HasActiveAccessAsync(Guid organizationId)
        {
            try
            {
                var sub = await _unitOfWork.Subscriptions.GetByOrganizationIdAsync(organizationId);
                if (sub == null) return false;
                return sub.IsFreePlan || sub.IsActive;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error checking active access"); return false; }
        }
    }
}
