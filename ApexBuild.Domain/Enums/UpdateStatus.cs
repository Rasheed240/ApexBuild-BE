namespace ApexBuild.Domain.Enums
{
    public enum UpdateStatus
    {
        Submitted = 1,
        UnderSupervisorReview = 2,
        SupervisorApproved = 3,
        SupervisorRejected = 4,
        UnderAdminReview = 5,
        AdminApproved = 6,
        AdminRejected = 7
    }
}