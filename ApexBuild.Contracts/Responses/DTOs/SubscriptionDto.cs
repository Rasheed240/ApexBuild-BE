
namespace ApexBuild.Contracts.Responses.DTOs
{
    public class SubscriptionDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }

        // License Information
        public int NumberOfLicenses { get; set; }
        public int LicensesUsed { get; set; }
        public decimal LicenseCostPerMonth { get; set; }
        public decimal TotalMonthlyAmount { get; set; }

        // Subscription Status
        public string Status { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;

        // Billing Period
        public DateTime BillingStartDate { get; set; }
        public DateTime BillingEndDate { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public DateTime? RenewalDate { get; set; }

        // Stripe Information
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public string? StripeSubscriptionItemId { get; set; }
        public string? StripePriceId { get; set; }
        public string? StripePaymentMethodId { get; set; }
        public DateTime? StripeCurrentPeriodStart { get; set; }
        public DateTime? StripeCurrentPeriodEnd { get; set; }

        // Payment Information
        public bool AutoRenew { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }

        // Trial Information
        public bool IsTrialPeriod { get; set; }
        public DateTime? TrialEndDate { get; set; }

        // Computed Properties
        public int RemainingLicenses { get; set; }
        public bool IsActive { get; set; }
        public bool HasExpired { get; set; }
        public bool IsLowOnLicenses { get; set; }
    }
}
