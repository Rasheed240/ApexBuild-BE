using System;
using System.Collections.Generic;
using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Domain.Entities
{
    /// <summary>
    /// Represents an organization's subscription/license.
    /// Each organization must have an active subscription.
    /// A user can have different subscriptions in different organizations.
    /// </summary>
    public class Subscription : BaseAuditableEntity, ISoftDelete
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; } // Owner/admin who purchased
        
        // License Information
        public int NumberOfLicenses { get; set; } // Total licenses purchased for org
        public int LicensesUsed { get; set; } // Currently assigned licenses
        public decimal LicenseCostPerMonth { get; set; } = 10m; // $10 per license
        public decimal TotalMonthlyAmount => NumberOfLicenses * LicenseCostPerMonth;
        
        // Subscription Status
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
        public SubscriptionBillingCycle BillingCycle { get; set; } = SubscriptionBillingCycle.Monthly;
        
        // Billing Period
        public DateTime BillingStartDate { get; set; }
        public DateTime BillingEndDate { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public DateTime? RenewalDate { get; set; }
        
        // Stripe Information
        public string? StripeCustomerId { get; set; } // Stripe customer ID for this organization
        public string? StripeSubscriptionId { get; set; } // Stripe subscription ID (required for subscriptions)
        public string? StripeSubscriptionItemId { get; set; } // Stripe subscription item ID for quantity updates
        public string? StripePriceId { get; set; } // Stripe price ID being used
        public string? StripePaymentMethodId { get; set; } // Default payment method on file
        public DateTime? StripeCurrentPeriodStart { get; set; } // From Stripe subscription
        public DateTime? StripeCurrentPeriodEnd { get; set; } // From Stripe subscription
        
        // Payment Information
        public bool AutoRenew { get; set; } = true;
        public decimal Amount { get; set; } // Total subscription amount
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        
        // Trial Information
        public bool IsTrialPeriod { get; set; } = false;
        public DateTime? TrialEndDate { get; set; }
        
        // Metadata
        public Dictionary<string, object>? MetaData { get; set; }
        
        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }
        
        // Navigation Properties
        public virtual Organization Organization { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual ICollection<OrganizationLicense> OrganizationLicenses { get; set; } = new List<OrganizationLicense>();
        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
        
        // Computed Properties
        public int RemainingLicenses => NumberOfLicenses - LicensesUsed;
        public bool IsActive => Status == SubscriptionStatus.Active && BillingEndDate > DateTime.UtcNow;
        public bool HasExpired => BillingEndDate <= DateTime.UtcNow;
        public bool IsLowOnLicenses => RemainingLicenses < 5; // Alert if less than 5 licenses
    }
}
