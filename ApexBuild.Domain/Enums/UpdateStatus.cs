namespace ApexBuild.Domain.Enums
{
    /// <summary>
    /// Tracks the approval chain for a task update submission.
    ///
    /// Contracted task chain (task has a ContractorId):
    ///   FieldWorker submits → ContractorAdmin reviews → DepartmentSupervisor reviews → ProjectAdmin approves
    ///
    /// Non-contracted task chain:
    ///   FieldWorker submits → DepartmentSupervisor reviews → ProjectAdmin approves
    /// </summary>
    public enum UpdateStatus
    {
        Submitted = 1,

        // Contractor Admin review (only for tasks under a contractor)
        UnderContractorAdminReview = 2,
        ContractorAdminApproved = 3,
        ContractorAdminRejected = 4,

        // Department Supervisor review
        UnderSupervisorReview = 5,
        SupervisorApproved = 6,
        SupervisorRejected = 7,

        // Project Admin final approval
        UnderAdminReview = 8,
        AdminApproved = 9,
        AdminRejected = 10
    }
}
