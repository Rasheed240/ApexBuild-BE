namespace ApexBuild.Infrastructure.Configurations
{
    /// <summary>
    /// Stripe configuration settings.
    /// Load these from appsettings.json under "StripeSettings" section.
    /// </summary>
    public class StripeSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public decimal MonthlyLicenseCost { get; set; } = 10m;
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMinutes { get; set; } = 15;
        
        // Stripe Subscription Configuration
        public string ProductId { get; set; } = string.Empty; // Stripe Product ID
        public string MonthlyPriceId { get; set; } = string.Empty; // Stripe Price ID for monthly billing
        public string? AnnualPriceId { get; set; } // Optional: Stripe Price ID for annual billing
    }
}
