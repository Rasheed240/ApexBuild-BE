namespace ApexBuild.Domain.Enums
{
    public enum NotificationType
    {
        // Invitations
        InvitationReceived = 1,
        InvitationAccepted = 2,

        // Task events
        TaskAssigned = 3,
        TaskStatusChanged = 4,
        TaskDueSoon = 5,
        TaskOverdue = 6,

        // Task update / review events
        UpdateSubmitted = 7,
        UpdateApproved = 8,
        UpdateRejected = 9,
        UpdateRequiresRevision = 10,

        // Project events
        ProjectStatusChanged = 11,
        ProjectCreated = 12,
        AddedToProject = 13,

        // Milestone events
        MilestoneReached = 14,
        MilestoneOverdue = 15,

        // Contractor events
        ContractorAssigned = 16,
        ContractExpiringSoon = 17,

        // Scheduling / reminders
        DeadlineReminder = 18,
        PendingApproval = 19,
        DailyUpdateReminder = 20,
        WeeklyProgressReport = 21,
        BirthdayNotification = 22,

        // Legacy / general
        TaskUpdate = 23,
        TaskReview = 24,
        SystemAlert = 25
    }
}
