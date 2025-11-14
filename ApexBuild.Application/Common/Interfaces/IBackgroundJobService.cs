using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApexBuild.Application.Common.Interfaces
{
    /// <summary>
    /// Interface for background job scheduling and management.
    /// Uses Hangfire for reliable background job processing.
    /// </summary>
    public interface IBackgroundJobService
    {
        /// <summary>
        /// Schedules subscription renewal job to run at a specific time.
        /// </summary>
        string ScheduleSubscriptionRenewal(Guid subscriptionId, DateTime scheduledTime);

        /// <summary>
        /// Schedules a payment retry job.
        /// </summary>
        string SchedulePaymentRetry(Guid paymentTransactionId, DateTime scheduledTime);

        /// <summary>
        /// Schedules license expiration notification.
        /// </summary>
        string ScheduleLicenseExpirationNotification(Guid licenseId, DateTime scheduledTime);

        /// <summary>
        /// Enqueues immediate subscription renewal.
        /// </summary>
        string EnqueueSubscriptionRenewal(Guid subscriptionId);

        /// <summary>
        /// Enqueues immediate payment retry.
        /// </summary>
        string EnqueuePaymentRetry(Guid paymentTransactionId);

        /// <summary>
        /// Sets up recurring job for daily subscription renewal checks.
        /// </summary>
        void SetupDailySubscriptionRenewalCheck();

        /// <summary>
        /// Sets up recurring job for checking failed payments.
        /// </summary>
        void SetupHourlyFailedPaymentCheck();

        /// <summary>
        /// Sets up recurring job for license expiration checks.
        /// </summary>
        void SetupDailyLicenseExpirationCheck();

        /// <summary>
        /// Deletes a scheduled job.
        /// </summary>
        bool DeleteScheduledJob(string jobId);

        /// <summary>
        /// Gets job details.
        /// </summary>
        Task<JobDetailsDto> GetJobDetailsAsync(string jobId);
    }

    /// <summary>
    /// Job details DTO.
    /// </summary>
    public class JobDetailsDto
    {
        public string JobId { get; set; }
        public string State { get; set; } // Scheduled, Processing, Succeeded, Failed, Deleted
        public DateTime? CreatedAt { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string ErrorMessage { get; set; }
    }
}
