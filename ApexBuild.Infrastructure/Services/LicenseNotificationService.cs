using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ApexBuild.Application.Common.Interfaces;

namespace ApexBuild.Infrastructure.Services
{
    /// <summary>License notifications are no longer used (license model removed). Kept as stub for DI compatibility.</summary>
    public class LicenseNotificationService : ILicenseNotificationService
    {
        private readonly ILogger<LicenseNotificationService> _logger;

        public LicenseNotificationService(ILogger<LicenseNotificationService> logger)
        {
            _logger = logger;
        }

        public Task SendLicenseExpirationNotificationAsync(Guid licenseId)
        {
            _logger.LogInformation("License notifications disabled (license model removed)");
            return Task.CompletedTask;
        }

        public Task CheckAndNotifyExpiringLicensesAsync()
        {
            return Task.CompletedTask;
        }

        public Task SendLicenseAssignedNotificationAsync(Guid licenseId)
        {
            return Task.CompletedTask;
        }

        public Task SendLicenseRevokedNotificationAsync(Guid licenseId)
        {
            return Task.CompletedTask;
        }
    }
}
