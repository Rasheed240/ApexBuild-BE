using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Infrastructure.Services
{
    public class LicenseNotificationService : ILicenseNotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<LicenseNotificationService> _logger;

        public LicenseNotificationService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<LicenseNotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task SendLicenseExpirationNotificationAsync(Guid licenseId)
        {
            try
            {
                var license = await _unitOfWork.OrganizationLicenses.GetByIdAsync(licenseId);
                if (license == null)
                {
                    return;
                }

                var subject = $"License Expiration Notice - {license.Organization.Name}";
                var message = $@"
Your license in {license.Organization.Name} is about to expire.

License Details:
- License Type: {license.LicenseType}
- Expires On: {license.ValidUntil:yyyy-MM-dd}
- Days Until Expiration: {license.DaysUntilExpiration}

Please ensure your subscription is renewed before this date to maintain access.
";

                await _emailService.SendEmailAsync(
                    license.User.Email,
                    subject,
                    message);

                _logger.LogInformation(
                    "License expiration notification sent to {Email} for license {LicenseId}",
                    license.User.Email, licenseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending license expiration notification for license {LicenseId}",
                    licenseId);
            }
        }

        public async Task CheckAndNotifyExpiringLicensesAsync()
        {
            try
            {
                _logger.LogInformation("Starting daily license expiration notification check");

                var expiringLicenses = await _unitOfWork.OrganizationLicenses.GetExpiringLicensesAsync(7);

                _logger.LogInformation(
                    "Found {Count} licenses expiring within 7 days",
                    expiringLicenses.Count);

                foreach (var license in expiringLicenses)
                {
                    await SendLicenseExpirationNotificationAsync(license.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking and notifying expiring licenses");
            }
        }

        public async Task SendLicenseAssignedNotificationAsync(Guid licenseId)
        {
            try
            {
                var license = await _unitOfWork.OrganizationLicenses.GetByIdAsync(licenseId);
                if (license == null)
                {
                    return;
                }

                var subject = $"License Assigned - {license.Organization.Name}";
                var message = $@"
A license has been assigned to you in {license.Organization.Name}.

License Details:
- License Type: {license.LicenseType}
- Valid From: {license.ValidFrom:yyyy-MM-dd}
- Valid Until: {license.ValidUntil:yyyy-MM-dd}
- License Key: {license.LicenseKey}

You can now access the platform with your account.
";

                await _emailService.SendEmailAsync(
                    license.User.Email,
                    subject,
                    message);

                _logger.LogInformation(
                    "License assigned notification sent to {Email} for license {LicenseId}",
                    license.User.Email, licenseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending license assigned notification for license {LicenseId}",
                    licenseId);
            }
        }

        public async Task SendLicenseRevokedNotificationAsync(Guid licenseId)
        {
            try
            {
                var license = await _unitOfWork.OrganizationLicenses.GetByIdAsync(licenseId);
                if (license == null)
                {
                    return;
                }

                var subject = $"License Revoked - {license.Organization.Name}";
                var message = $@"
Your license in {license.Organization.Name} has been revoked.

License Details:
- License Key: {license.LicenseKey}
- Revocation Reason: {license.RevocationReason}
- Revoked At: {license.RevokedAt:yyyy-MM-dd HH:mm:ss}

You will no longer have access to the platform.

If you believe this is a mistake, please contact the organization administrator.
";

                await _emailService.SendEmailAsync(
                    license.User.Email,
                    subject,
                    message);

                _logger.LogInformation(
                    "License revoked notification sent to {Email} for license {LicenseId}",
                    license.User.Email, licenseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending license revoked notification for license {LicenseId}",
                    licenseId);
            }
        }
    }
}
