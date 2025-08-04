using System;
using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Domain.Entities
{
    /// <summary>
    /// Represents a payment transaction for a subscription.
    /// Tracks all payment attempts, successful charges, and failed transactions.
    /// </summary>
    public class PaymentTransaction : BaseAuditableEntity, ISoftDelete
    {
        public Guid OrganizationId { get; set; }
        public Guid SubscriptionId { get; set; }
        public Guid UserId { get; set; } // User who initiated the payment
        
        // Transaction Information
        public string TransactionId { get; set; } = string.Empty; // Reference ID from system
        public string? StripeChargeId { get; set; } // Stripe charge ID
        public PaymentType PaymentType { get; set; } // Initial, Renewal, Manual, Refund
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CreditCard;
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        
        // Amount Information
        public decimal Amount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal TotalAmount => Amount + (TaxAmount ?? 0) - (DiscountAmount ?? 0);
        
        // Invoice Information
        public string? InvoiceNumber { get; set; }
        public string? InvoiceUrl { get; set; } // URL to invoice PDF
        public string? ReceiptUrl { get; set; } // URL to receipt
        
        // Dates
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public DateTime? RefundedAt { get; set; }
        public decimal? RefundAmount { get; set; }
        public string? RefundReason { get; set; }
        
        // Card Information (masked for security)
        public string? CardLast4 { get; set; } // Last 4 digits of card
        public string? CardBrand { get; set; } // Visa, Mastercard, etc.
        public int? CardExpiryMonth { get; set; }
        public int? CardExpiryYear { get; set; }
        
        // Error Handling
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; } = 0;
        public DateTime? NextRetryAt { get; set; }
        public int MaxRetries { get; set; } = 3;
        
        // Description
        public string Description { get; set; } = string.Empty; // What was charged for
        
        // Metadata
        public Dictionary<string, object>? MetaData { get; set; }
        
        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }
        
        // Navigation Properties
        public virtual Organization Organization { get; set; } = null!;
        public virtual Subscription Subscription { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        
        // Computed Properties
        public bool IsSuccessful => Status == PaymentStatus.Completed;
        public bool IsFailed => Status == PaymentStatus.Failed;
        public bool IsRefunded => RefundedAt.HasValue;
        public bool CanRetry => IsFailed && RetryCount < MaxRetries && NextRetryAt <= DateTime.UtcNow;
    }
}
