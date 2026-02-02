using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Domain.Entities
{
    /// <summary>
    /// Represents an organization's subscription to ApexBuild.
    ///
    /// Billing model:
    /// - Flat $20 per active user per month (UserMonthlyRate).
    /// - Organizations created by a SuperAdmin are free (IsFreePlan = true).
    /// - Monthly amount is computed as: active project member count × UserMonthlyRate.
    ///
    /// Stripe handles actual payment processing. This entity stores the billing state.
    /// </summary>
    public class Subscription : BaseAuditableEntity, ISoftDelete
    {
        public Guid OrganizationId { get; set; }

        /// <summary>PlatformAdmin / owner who set up billing</summary>
        public Guid UserId { get; set; }

        // Pricing
        /// <summary>Flat per-user monthly rate. Default $20.</summary>
        public decimal UserMonthlyRate { get; set; } = 20m;

        /// <summary>Number of currently active users in the organization's projects</summary>
        public int ActiveUserCount { get; set; } = 0;

        /// <summary>Computed: ActiveUserCount × UserMonthlyRate</summary>
        public decimal TotalMonthlyAmount => ActiveUserCount * UserMonthlyRate;

        /// <summary>
        /// If true, this organization was created by a SuperAdmin and users are not billed.
        /// </summary>
        public bool IsFreePlan { get; set; } = false;

        // Subscription Status
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
        public SubscriptionBillingCycle BillingCycle { get; set; } = SubscriptionBillingCycle.Monthly;

        // Billing Period
        public DateTime BillingStartDate { get; set; }
        public DateTime BillingEndDate { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public DateTime? RenewalDate { get; set; }

        // Stripe Integration
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public string? StripeSubscriptionItemId { get; set; }
        public string? StripePriceId { get; set; }
        public string? StripePaymentMethodId { get; set; }
        public DateTime? StripeCurrentPeriodStart { get; set; }
        public DateTime? StripeCurrentPeriodEnd { get; set; }

        // Payment
        public bool AutoRenew { get; set; } = true;
        public decimal Amount { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }

        // Trial
        public bool IsTrialPeriod { get; set; } = false;
        public DateTime? TrialEndDate { get; set; }

        public Dictionary<string, object>? MetaData { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        // Navigation Properties
        public virtual Organization Organization { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

        // Computed
        public bool IsActive => Status == SubscriptionStatus.Active && BillingEndDate > DateTime.UtcNow;
        public bool HasExpired => BillingEndDate <= DateTime.UtcNow;
    }
}
