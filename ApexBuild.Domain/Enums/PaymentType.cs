namespace ApexBuild.Domain.Enums
{
    public enum PaymentType
    {
        Initial = 1,      // First subscription payment
        Renewal = 2,      // Monthly/recurring renewal
        Manual = 3,       // Manual invoice payment
        Refund = 4,       // Refund transaction
        Adjustment = 5,   // Charge adjustment
        Trial = 6         // Trial payment (usually free)
    }
}
