using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Common.Interfaces
{
    /// <summary>
    /// Manages organization subscriptions.
    /// Billing model: $20/active user/month. SuperAdmin projects are free.
    /// </summary>
    public interface ISubscriptionService
    {
        /// <summary>Creates or activates a subscription for an organization.</summary>
        Task<(bool Success, Subscription Subscription, string ErrorMessage)> CreateSubscriptionAsync(
            Guid organizationId,
            Guid userId,
            bool isFreePlan = false,
            int trialDays = 0);

        /// <summary>Cancels an organization subscription.</summary>
        Task<(bool Success, string ErrorMessage)> CancelSubscriptionAsync(
            Guid subscriptionId,
            string reason);

        /// <summary>Renews a subscription for another billing period.</summary>
        Task<(bool Success, string ErrorMessage)> RenewSubscriptionAsync(Guid subscriptionId);

        /// <summary>Reactivates a cancelled or expired subscription.</summary>
        Task<(bool Success, string ErrorMessage)> ReactivateSubscriptionAsync(Guid subscriptionId);

        /// <summary>Gets the subscription for an organization.</summary>
        Task<Subscription?> GetSubscriptionAsync(Guid organizationId);

        /// <summary>Gets billing summary for an organization.</summary>
        Task<SubscriptionStatsDto> GetSubscriptionStatsAsync(Guid organizationId);

        /// <summary>Updates the active user count for billing.</summary>
        Task UpdateActiveUserCountAsync(Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>Gets subscriptions expiring within the given days.</summary>
        Task<List<Subscription>> GetExpiringSubscriptionsAsync(int daysUntilExpiration = 7);

        /// <summary>Gets all expired subscriptions.</summary>
        Task<List<Subscription>> GetExpiredSubscriptionsAsync();

        /// <summary>Returns whether an organization has active access (subscription or free plan).</summary>
        Task<bool> HasActiveAccessAsync(Guid organizationId);
    }

    public class SubscriptionStatsDto
    {
        public Guid OrganizationId { get; set; }
        public int ActiveUserCount { get; set; }
        public decimal UserMonthlyRate { get; set; }
        public decimal TotalMonthlyAmount { get; set; }
        public bool IsFreePlan { get; set; }
        public DateTime BillingStartDate { get; set; }
        public DateTime BillingEndDate { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public SubscriptionStatus Status { get; set; }
        public bool IsActive { get; set; }
        public int DaysUntilRenewal { get; set; }
        public bool IsExpiringSoon { get; set; }
    }
}
