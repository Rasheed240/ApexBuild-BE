using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Common.Interfaces
{
    /// <summary>
    /// Interface for managing subscriptions and licenses.
    /// Handles license assignment, validation, and subscription management.
    /// </summary>
    public interface ISubscriptionService
    {
        /// <summary>
        /// Creates a new subscription for an organization.
        /// </summary>
        Task<(bool Success, Subscription Subscription, string ErrorMessage)> CreateSubscriptionAsync(
            Guid organizationId,
            Guid userId,
            int numberOfLicenses,
            int trialDays = 0);

        /// <summary>
        /// Assigns a license to a user in an organization.
        /// Checks if organization has available licenses.
        /// </summary>
        Task<(bool Success, OrganizationLicense License, string ErrorMessage)> AssignLicenseAsync(
            Guid organizationId,
            Guid userId);

        /// <summary>
        /// Revokes a license for a user.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> RevokeLicenseAsync(
            Guid licenseId,
            string reason);

        /// <summary>
        /// Checks if a user has an active license in an organization.
        /// </summary>
        Task<(bool HasLicense, OrganizationLicense License)> HasActiveLicenseAsync(
            Guid organizationId,
            Guid userId);

        /// <summary>
        /// Validates if a user can access an organization based on license.
        /// </summary>
        Task<bool> ValidateLicenseAccessAsync(Guid organizationId, Guid userId);

        /// <summary>
        /// Gets all licenses for an organization.
        /// </summary>
        Task<List<OrganizationLicense>> GetOrganizationLicensesAsync(
            Guid organizationId,
            LicenseStatus? status = null);

        /// <summary>
        /// Gets all licenses for a user.
        /// </summary>
        Task<List<OrganizationLicense>> GetUserLicensesAsync(Guid userId);

        /// <summary>
        /// Gets subscription for an organization.
        /// </summary>
        Task<Subscription> GetSubscriptionAsync(Guid organizationId);

        /// <summary>
        /// Upgrades subscription to add more licenses.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> UpgradeSubscriptionAsync(
            Guid subscriptionId,
            int additionalLicenses);

        /// <summary>
        /// Downgrades subscription to reduce licenses.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> DowngradeSubscriptionAsync(
            Guid subscriptionId,
            int licensesToRemove);

        /// <summary>
        /// Renews a subscription for another billing period.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> RenewSubscriptionAsync(Guid subscriptionId);

        /// <summary>
        /// Cancels a subscription.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> CancelSubscriptionAsync(
            Guid subscriptionId,
            string reason);

        /// <summary>
        /// Reactivates a cancelled subscription.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> ReactivateSubscriptionAsync(Guid subscriptionId);

        /// <summary>
        /// Gets subscription statistics.
        /// </summary>
        Task<SubscriptionStatsDto> GetSubscriptionStatsAsync(Guid organizationId);

        /// <summary>
        /// Gets all expired subscriptions that need renewal.
        /// </summary>
        Task<List<Subscription>> GetExpiredSubscriptionsAsync();

        /// <summary>
        /// Gets subscriptions expiring soon (within 7 days).
        /// </summary>
        Task<List<Subscription>> GetExpiringSubscriptionsAsync(int daysUntilExpiration = 7);

        /// <summary>
        /// Gets all users with active licenses in an organization.
        /// </summary>
        Task<List<(User User, OrganizationLicense License)>> GetOrganiztionActiveUsersWithLicensesAsync(Guid organizationId);
    }

    /// <summary>
    /// Subscription statistics DTO.
    /// </summary>
    public class SubscriptionStatsDto
    {
        public Guid OrganizationId { get; set; }
        public int TotalLicenses { get; set; }
        public int UsedLicenses { get; set; }
        public int AvailableLicenses { get; set; }
        public decimal MonthlyAmount { get; set; }
        public DateTime BillingStartDate { get; set; }
        public DateTime BillingEndDate { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public SubscriptionStatus Status { get; set; }
        public bool IsActive { get; set; }
        public int DaysUntilRenewal { get; set; }
        public bool IsExpiringSoon { get; set; }
        public decimal LicenseCostPerMonth { get; set; }
    }
}
