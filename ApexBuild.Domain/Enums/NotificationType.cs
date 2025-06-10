namespace ApexBuild.Domain.Enums
{
    public enum NotificationType
    {
        InvitationReceived = 1,
        TaskAssigned = 2,
        UpdateSubmitted = 3,
        UpdateApproved = 4,
        UpdateRejected = 5,
        ProjectStatusChanged = 6,
        DeadlineReminder = 7,
        PendingApproval = 8,
        DailyUpdateReminder = 9,
        BirthdayNotification = 10,
        WeeklyProgressReport = 11,
        TaskUpdate = 12,
        TaskReview = 13
    }
}