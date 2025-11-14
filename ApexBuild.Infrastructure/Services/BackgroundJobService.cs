using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Hangfire;
using ApexBuild.Application.Common.Interfaces;

namespace ApexBuild.Infrastructure.Services
{
    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly ILogger<BackgroundJobService> _logger;
        private readonly IRecurringJobManager _recurringJobManager;

        public BackgroundJobService(
            ILogger<BackgroundJobService> logger,
            IRecurringJobManager recurringJobManager)
        {
            _logger = logger;
            _recurringJobManager = recurringJobManager;
        }

        public string ScheduleSubscriptionRenewal(Guid subscriptionId, DateTime scheduledTime)
        {
            try
            {
                var jobId = BackgroundJob.Schedule<ISubscriptionBillingService>(
                    x => x.ProcessSubscriptionRenewalAsync(subscriptionId),
                    scheduledTime);

                _logger.LogInformation(
                    "Scheduled subscription renewal for {SubscriptionId} at {ScheduledTime}. JobId: {JobId}",
                    subscriptionId, scheduledTime, jobId);

                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error scheduling subscription renewal for {SubscriptionId}",
                    subscriptionId);
                throw;
            }
        }

        public string SchedulePaymentRetry(Guid paymentTransactionId, DateTime scheduledTime)
        {
            try
            {
                var jobId = BackgroundJob.Schedule<IPaymentProcessingService>(
                    x => x.RetryPaymentAsync(paymentTransactionId),
                    scheduledTime);

                _logger.LogInformation(
                    "Scheduled payment retry for {PaymentTransactionId} at {ScheduledTime}. JobId: {JobId}",
                    paymentTransactionId, scheduledTime, jobId);

                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error scheduling payment retry for {PaymentTransactionId}",
                    paymentTransactionId);
                throw;
            }
        }

        public string ScheduleLicenseExpirationNotification(Guid licenseId, DateTime scheduledTime)
        {
            try
            {
                var jobId = BackgroundJob.Schedule<ILicenseNotificationService>(
                    x => x.SendLicenseExpirationNotificationAsync(licenseId),
                    scheduledTime);

                _logger.LogInformation(
                    "Scheduled license expiration notification for {LicenseId} at {ScheduledTime}. JobId: {JobId}",
                    licenseId, scheduledTime, jobId);

                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error scheduling license expiration notification for {LicenseId}",
                    licenseId);
                throw;
            }
        }

        public string EnqueueSubscriptionRenewal(Guid subscriptionId)
        {
            try
            {
                var jobId = BackgroundJob.Enqueue<ISubscriptionBillingService>(
                    x => x.ProcessSubscriptionRenewalAsync(subscriptionId));

                _logger.LogInformation(
                    "Enqueued subscription renewal for {SubscriptionId}. JobId: {JobId}",
                    subscriptionId, jobId);

                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error enqueuing subscription renewal for {SubscriptionId}",
                    subscriptionId);
                throw;
            }
        }

        public string EnqueuePaymentRetry(Guid paymentTransactionId)
        {
            try
            {
                var jobId = BackgroundJob.Enqueue<IPaymentProcessingService>(
                    x => x.RetryPaymentAsync(paymentTransactionId));

                _logger.LogInformation(
                    "Enqueued payment retry for {PaymentTransactionId}. JobId: {JobId}",
                    paymentTransactionId, jobId);

                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error enqueuing payment retry for {PaymentTransactionId}",
                    paymentTransactionId);
                throw;
            }
        }

        public void SetupDailySubscriptionRenewalCheck()
        {
            try
            {
                _recurringJobManager.AddOrUpdate<ISubscriptionBillingService>(
                    "daily-subscription-renewal-check",
                    x => x.ProcessExpiringSubscriptionsAsync(),
                    Cron.Daily(2, 0)); // Run daily at 2:00 AM

                _logger.LogInformation("Daily subscription renewal check scheduled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up daily subscription renewal check");
                throw;
            }
        }

        public void SetupHourlyFailedPaymentCheck()
        {
            try
            {
                _recurringJobManager.AddOrUpdate<IPaymentProcessingService>(
                    "hourly-failed-payment-check",
                    x => x.ProcessFailedPaymentsAsync(),
                    Cron.Hourly(15)); // Run hourly at :15 minutes

                _logger.LogInformation("Hourly failed payment check scheduled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up hourly failed payment check");
                throw;
            }
        }

        public void SetupDailyLicenseExpirationCheck()
        {
            try
            {
                _recurringJobManager.AddOrUpdate<ILicenseNotificationService>(
                    "daily-license-expiration-check",
                    x => x.CheckAndNotifyExpiringLicensesAsync(),
                    Cron.Daily(3, 0)); // Run daily at 3:00 AM

                _logger.LogInformation("Daily license expiration check scheduled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up daily license expiration check");
                throw;
            }
        }

        public bool DeleteScheduledJob(string jobId)
        {
            try
            {
                BackgroundJob.Delete(jobId);
                _logger.LogInformation("Deleted job {JobId}", jobId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job {JobId}", jobId);
                return false;
            }
        }

        public async Task<JobDetailsDto> GetJobDetailsAsync(string jobId)
        {
            // Note: Getting job details from Hangfire requires accessing the storage directly
            // This is a placeholder - actual implementation would depend on your Hangfire storage
            return await Task.FromResult(new JobDetailsDto
            {
                JobId = jobId,
                State = "Unknown"
            });
        }
    }
}
