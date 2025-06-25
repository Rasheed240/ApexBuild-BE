using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using RazorLight;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ApexBuild.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IRazorLightEngine _razorEngine;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _razorEngine = new RazorLightEngineBuilder()
                .UseEmbeddedResourcesProject(typeof(EmailService))
                .UseMemoryCachingProvider()
                .Build();
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            var useSendGrid = _configuration.GetValue<bool>("Email:UseSendGrid");

            if (useSendGrid)
            {
                await SendViaSendGridAsync(to, subject, body);
            }
            else
            {
                await SendViaSmtpAsync(to, subject, body, isHtml);
            }
        }

        private async Task SendViaSendGridAsync(string to, string subject, string body)
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(
                _configuration["Email:FromAddress"],
                _configuration["Email:FromName"]);
            var toAddress = new EmailAddress(to);

            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, body, body);
            await client.SendEmailAsync(msg);
        }

        private async Task SendViaSmtpAsync(string to, string subject, string body, bool isHtml)
        {
            using var smtpClient = new System.Net.Mail.SmtpClient(_configuration["Smtp:Host"])
            {
                Port = int.Parse(_configuration["Smtp:Port"]!),
                Credentials = new System.Net.NetworkCredential(
                    _configuration["Smtp:Username"],
                    _configuration["Smtp:Password"]),
                EnableSsl = true
            };

            var mailMessage = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(
                    _configuration["Email:FromAddress"]!,
                    _configuration["Email:FromName"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            mailMessage.To.Add(to);
            await smtpClient.SendMailAsync(mailMessage);
        }

        public async Task SendEmailConfirmationAsync(string email, string fullName, string confirmationToken)
        {
            var confirmUrl = $"{_configuration["App:BaseUrl"]}/auth/confirm-email?token={confirmationToken}";
            var body = EmailTemplates.GetEmailConfirmationTemplate(fullName, confirmUrl);
            await SendEmailAsync(email, "Confirm Your Email Address - ApexBuild", body);
        }

        public async Task SendPasswordResetAsync(string email, string fullName, string resetToken)
        {
            var resetUrl = $"{_configuration["App:BaseUrl"]}/auth/reset-password?token={resetToken}";
            var body = EmailTemplates.GetPasswordResetTemplate(fullName, resetUrl);
            await SendEmailAsync(email, "Reset Your Password - ApexBuild", body);
        }

        public async Task SendInvitationAsync(string email, string inviterName, string roleName, string? projectName, string invitationUrl, string? message)
        {
            var body = EmailTemplates.GetInvitationTemplate(inviterName, roleName, projectName, invitationUrl, message);
            await SendEmailAsync(email, $"You've Been Invited to Join ApexBuild - {inviterName}", body);
        }

        public async Task SendTaskAssignedAsync(string email, string fullName, string taskTitle, string projectName)
        {
            var body = EmailTemplates.GetTaskAssignedTemplate(fullName, taskTitle, projectName, _configuration["App:BaseUrl"] ?? "");
            await SendEmailAsync(email, $"New Task Assigned: {taskTitle}", body);
        }

        public async Task SendUpdateSubmittedAsync(string email, string fullName, string taskTitle, string submitterName)
        {
            var body = EmailTemplates.GetUpdateSubmittedTemplate(fullName, taskTitle, submitterName, _configuration["App:BaseUrl"] ?? "");
            await SendEmailAsync(email, $"New Daily Report Submitted: {taskTitle}", body);
        }

        public async Task SendUpdateReviewedAsync(string email, string fullName, string taskTitle, bool approved, string? feedback)
        {
            var body = EmailTemplates.GetUpdateReviewedTemplate(fullName, taskTitle, approved, feedback);
            var status = approved ? "Approved" : "Needs Revision";
            await SendEmailAsync(email, $"Daily Report {status}: {taskTitle}", body);
        }

        public async Task SendDeadlineReminderAsync(string email, string fullName, string taskTitle, DateTime dueDate)
        {
            var body = EmailTemplates.GetDeadlineReminderTemplate(fullName, taskTitle, dueDate, _configuration["App:BaseUrl"] ?? "");
            await SendEmailAsync(email, $"Deadline Reminder: {taskTitle}", body);
        }
        
        public async Task SendDailyUpdateReminderAsync(string email, string fullName, int taskCount, string tasksList)
        {
            var body = EmailTemplates.GetDailyUpdateReminderTemplate(fullName, taskCount, tasksList, _configuration["App:BaseUrl"] ?? "");
            await SendEmailAsync(email, "Daily Work Update Reminder - ApexBuild", body);
        }

        public async Task SendWeeklyProgressReportAsync(
            string email,
            string fullName,
            string projectName,
            int totalTasks,
            int completedTasks,
            int updatesLastWeek,
            int completedTasksLastWeek,
            double progressPercentage)
        {
            var body = EmailTemplates.GetWeeklyProgressReportTemplate(
                fullName,
                projectName,
                totalTasks,
                completedTasks,
                updatesLastWeek,
                completedTasksLastWeek,
                progressPercentage,
                _configuration["App:BaseUrl"] ?? ""
            );
            await SendEmailAsync(email, $"Weekly Progress Report - {projectName}", body);
        }
    }

}