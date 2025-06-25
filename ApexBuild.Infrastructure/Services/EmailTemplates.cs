namespace ApexBuild.Infrastructure.Services;

public static class EmailTemplates
{
    private const string LogoUrl = "https://via.placeholder.com/200x60/2563eb/ffffff?text=ApexBuild";
    private const string PrimaryColor = "#2563eb";
    private const string SuccessColor = "#10b981";
    private const string WarningColor = "#f59e0b";
    private const string ErrorColor = "#ef4444";

    public static string GetEmailTemplate(string title, string content, string buttonText = null, string buttonUrl = null)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: #f3f4f6;
            line-height: 1.6;
        }}
        .email-container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
        }}
        .email-header {{
            background: linear-gradient(135deg, {PrimaryColor} 0%, #1e40af 100%);
            padding: 40px 30px;
            text-align: center;
        }}
        .email-logo {{
            max-width: 200px;
            height: auto;
            margin-bottom: 20px;
        }}
        .email-body {{
            padding: 40px 30px;
            color: #1f2937;
        }}
        .email-title {{
            font-size: 24px;
            font-weight: 700;
            color: #111827;
            margin-bottom: 20px;
            line-height: 1.3;
        }}
        .email-content {{
            font-size: 16px;
            color: #4b5563;
            margin-bottom: 30px;
        }}
        .button-container {{
            text-align: center;
            margin: 30px 0;
        }}
        .email-button {{
            display: inline-block;
            padding: 14px 32px;
            background: linear-gradient(135deg, {PrimaryColor} 0%, #1e40af 100%);
            color: #ffffff !important;
            text-decoration: none;
            border-radius: 8px;
            font-weight: 600;
            font-size: 16px;
            box-shadow: 0 4px 6px rgba(37, 99, 235, 0.25);
            transition: all 0.3s ease;
        }}
        .email-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 12px rgba(37, 99, 235, 0.35);
        }}
        .link-fallback {{
            margin-top: 20px;
            padding: 15px;
            background-color: #f9fafb;
            border-radius: 6px;
            font-size: 14px;
            color: #6b7280;
            word-break: break-all;
        }}
        .email-footer {{
            background-color: #f9fafb;
            padding: 30px;
            text-align: center;
            border-top: 1px solid #e5e7eb;
        }}
        .footer-text {{
            font-size: 14px;
            color: #6b7280;
            margin-bottom: 10px;
        }}
        .footer-link {{
            color: {PrimaryColor};
            text-decoration: none;
        }}
        .divider {{
            height: 1px;
            background-color: #e5e7eb;
            margin: 30px 0;
        }}
        @media only screen and (max-width: 600px) {{
            .email-body {{
                padding: 30px 20px;
            }}
            .email-header {{
                padding: 30px 20px;
            }}
        }}
    </style>
</head>
<body>
    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"">
        <tr>
            <td>
                <div class=""email-container"">
                    <div class=""email-header"">
                        <img src=""{LogoUrl}"" alt=""ApexBuild Logo"" class=""email-logo"" />
                    </div>
                    <div class=""email-body"">
                        <h1 class=""email-title"">{title}</h1>
                        <div class=""email-content"">
                            {content}
                        </div>
                        {(buttonText != null && buttonUrl != null ? $@"
                        <div class=""button-container"">
                            <a href=""{buttonUrl}"" class=""email-button"">{buttonText}</a>
                        </div>
                        " : "")}
                        {(buttonUrl != null ? $@"
                        <div class=""link-fallback"">
                            <p style=""margin: 0; font-weight: 600; margin-bottom: 5px;"">Or copy and paste this link into your browser:</p>
                            <p style=""margin: 0; color: {PrimaryColor};"">{buttonUrl}</p>
                        </div>
                        " : "")}
                    </div>
                    <div class=""email-footer"">
                        <p class=""footer-text"">Best regards,<br><strong>The ApexBuild Team</strong></p>
                        <div class=""divider""></div>
                        <p class=""footer-text"">
                            © {DateTime.UtcNow.Year} ApexBuild. All rights reserved.<br>
                            <a href=""#"" class=""footer-link"">Privacy Policy</a> | 
                            <a href=""#"" class=""footer-link"">Terms of Service</a>
                        </p>
                    </div>
                </div>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    public static string GetEmailConfirmationTemplate(string fullName, string confirmationUrl)
    {
        var content = $@"
            <p>Welcome to <strong>ApexBuild</strong>, {fullName}!</p>
            <p>Thank you for registering with us. We're excited to have you on board and can't wait to help you manage your construction projects more efficiently.</p>
            <p>To complete your registration and activate your account, please confirm your email address by clicking the button below. This helps us ensure the security of your account.</p>
            <p style=""color: #6b7280; font-size: 14px; margin-top: 20px;"">
                <strong>Note:</strong> This confirmation link will expire in 24 hours. If you didn't create an account, you can safely ignore this email.
            </p>";

        return GetEmailTemplate(
            "Confirm Your Email Address",
            content,
            "Confirm Email Address",
            confirmationUrl
        );
    }

    public static string GetPasswordResetTemplate(string fullName, string resetUrl)
    {
        var content = $@"
            <p>Hello {fullName},</p>
            <p>We received a request to reset your password for your ApexBuild account.</p>
            <p>Click the button below to create a new password. If you didn't request this, you can safely ignore this email and your password will remain unchanged.</p>
            <p style=""color: #6b7280; font-size: 14px; margin-top: 20px;"">
                <strong>Security Tip:</strong> This link will expire in 24 hours for your protection.
            </p>";

        return GetEmailTemplate(
            "Reset Your Password",
            content,
            "Reset Password",
            resetUrl
        );
    }

    public static string GetInvitationTemplate(string inviterName, string roleName, string? projectName, string invitationUrl, string? message)
    {
        var projectInfo = projectName != null ? $" for the project <strong>{projectName}</strong>" : "";
        var customMessage = !string.IsNullOrEmpty(message) ? $@"<div style=""padding: 15px; background-color: #f0f9ff; border-left: 4px solid {PrimaryColor}; border-radius: 4px; margin: 20px 0;"">
                <p style=""margin: 0; font-style: italic; color: #1e40af;"">{message}</p>
            </div>" : "";

        var content = $@"
            <p>Hello,</p>
            <p><strong>{inviterName}</strong> has invited you to join ApexBuild as a <strong>{roleName}</strong>{projectInfo}.</p>
            {customMessage}
            <p>Click the button below to accept the invitation and get started with your team:</p>
            <p style=""color: #6b7280; font-size: 14px; margin-top: 20px;"">
                <strong>Note:</strong> This invitation will expire in 7 days.
            </p>";

        return GetEmailTemplate(
            $"You've Been Invited to Join ApexBuild",
            content,
            "Accept Invitation",
            invitationUrl
        );
    }

    public static string GetTaskAssignedTemplate(string fullName, string taskTitle, string projectName, string baseUrl)
    {
        var taskUrl = $"{baseUrl}/tasks";
        var content = $@"
            <p>Hello {fullName},</p>
            <p>A new task has been assigned to you in the project <strong>{projectName}</strong>:</p>
            <div style=""padding: 20px; background: linear-gradient(135deg, #f0f9ff 0%, #e0f2fe 100%); border-radius: 8px; margin: 20px 0; border-left: 4px solid {PrimaryColor};"">
                <h2 style=""margin: 0 0 10px 0; color: #111827; font-size: 20px;"">{taskTitle}</h2>
            </div>
            <p>Please log in to ApexBuild to view the full task details and get started.</p>";

        return GetEmailTemplate(
            $"New Task Assigned: {taskTitle}",
            content,
            "View Task",
            taskUrl
        );
    }

    public static string GetUpdateReviewedTemplate(string fullName, string taskTitle, bool approved, string? feedback)
    {
        var statusColor = approved ? SuccessColor : ErrorColor;
        var statusText = approved ? "Approved" : "Needs Revision";
        var statusIcon = approved ? "✓" : "⚠";
        var feedbackSection = !string.IsNullOrEmpty(feedback)
            ? $@"
            <div style=""padding: 15px; background-color: #fef3c7; border-left: 4px solid {WarningColor}; border-radius: 6px; margin: 20px 0;"">
                <p style=""margin: 0 0 5px 0; font-weight: 600; color: #92400e;"">Feedback:</p>
                <p style=""margin: 0; color: #78350f;"">{feedback}</p>
            </div>"
            : "";

        var content = $@"
            <p>Hello {fullName},</p>
            <p>Your daily report for the task <strong>{taskTitle}</strong> has been reviewed.</p>
            <div style=""padding: 20px; background-color: {(approved ? "#d1fae5" : "#fee2e2")}; border-radius: 8px; margin: 20px 0; text-align: center; border: 2px solid {statusColor};"">
                <div style=""font-size: 32px; margin-bottom: 10px;"">{statusIcon}</div>
                <h2 style=""margin: 0; color: {statusColor}; font-size: 22px; font-weight: 700;"">{statusText}</h2>
            </div>
            {feedbackSection}
            <p>{(approved ? "Great work! Keep up the excellent progress." : "Please review the feedback above and submit a revised update when ready.")}</p>";

        return GetEmailTemplate(
            $"Daily Report {statusText}: {taskTitle}",
            content
        );
    }

    public static string GetWeeklyProgressReportTemplate(
        string fullName,
        string projectName,
        int totalTasks,
        int completedTasks,
        int updatesLastWeek,
        int completedTasksLastWeek,
        double progressPercentage,
        string baseUrl)
    {
        var projectUrl = $"{baseUrl}/projects";
        var progressBarColor = progressPercentage >= 75 ? SuccessColor : progressPercentage >= 50 ? WarningColor : PrimaryColor;
        
        var content = $@"
            <p>Hello {fullName},</p>
            <p>Here's your weekly progress summary for <strong>{projectName}</strong>:</p>
            
            <div style=""padding: 25px; background: linear-gradient(135deg, #f0f9ff 0%, #e0f2fe 100%); border-radius: 10px; margin: 25px 0; border: 1px solid #bfdbfe;"">
                <h3 style=""margin: 0 0 20px 0; color: {PrimaryColor}; font-size: 20px; font-weight: 700;"">📊 Overall Project Status</h3>
                
                <div style=""margin-bottom: 25px;"">
                    <div style=""display: flex; justify-content: space-between; margin-bottom: 8px; font-weight: 600; color: #111827;"">
                        <span>Progress:</span>
                        <span>{progressPercentage:F1}%</span>
                    </div>
                    <div style=""background-color: #e5e7eb; border-radius: 10px; height: 24px; overflow: hidden; box-shadow: inset 0 2px 4px rgba(0,0,0,0.1);"">
                        <div style=""background: linear-gradient(90deg, {progressBarColor} 0%, {progressBarColor}dd 100%); height: 100%; width: {progressPercentage}%; transition: width 0.3s ease; display: flex; align-items: center; justify-content: center; color: white; font-weight: 700; font-size: 12px;"">
                            {progressPercentage:F0}%
                        </div>
                    </div>
                </div>
                
                <table style=""width: 100%; margin-top: 20px; border-collapse: collapse; background-color: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.05);"">
                    <tr style=""background-color: #f9fafb;"">
                        <td style=""padding: 12px 15px; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151;"">Total Tasks:</td>
                        <td style=""padding: 12px 15px; border-bottom: 1px solid #e5e7eb; text-align: right; font-weight: 700; color: #111827;"">{totalTasks}</td>
                    </tr>
                    <tr>
                        <td style=""padding: 12px 15px; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151;"">Completed Tasks:</td>
                        <td style=""padding: 12px 15px; border-bottom: 1px solid #e5e7eb; text-align: right; font-weight: 700; color: #111827;"">{completedTasks}</td>
                    </tr>
                    <tr style=""background-color: #eff6ff;"">
                        <td style=""padding: 12px 15px; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151;"">Updates Submitted (Last Week):</td>
                        <td style=""padding: 12px 15px; border-bottom: 1px solid #e5e7eb; text-align: right; font-weight: 700; color: {PrimaryColor};"">{updatesLastWeek}</td>
                    </tr>
                    <tr style=""background-color: #d1fae5;"">
                        <td style=""padding: 12px 15px; font-weight: 600; color: #374151;"">Tasks Completed (Last Week):</td>
                        <td style=""padding: 12px 15px; text-align: right; font-weight: 700; color: {SuccessColor};"">{completedTasksLastWeek}</td>
                    </tr>
                </table>
            </div>

            <p style=""font-size: 16px; font-weight: 600; color: #111827;"">Keep up the excellent work! Your team's dedication is driving this project forward. 🚀</p>";

        return GetEmailTemplate(
            $"Weekly Progress Report - {projectName}",
            content,
            "View Full Report",
            projectUrl
        );
    }

    public static string GetUpdateSubmittedTemplate(string fullName, string taskTitle, string submitterName, string baseUrl)
    {
        var taskUrl = $"{baseUrl}/tasks";
        var content = $@"
            <p>Hello {fullName},</p>
            <p><strong>{submitterName}</strong> has submitted a daily report for the task <strong>{taskTitle}</strong>.</p>
            <p>Please review the submission and approve or provide feedback.</p>
            <div style=""padding: 15px; background-color: #f0f9ff; border-left: 4px solid {PrimaryColor}; border-radius: 6px; margin: 20px 0;"">
                <p style=""margin: 0; font-weight: 600; color: #1e40af;"">Action Required:</p>
                <p style=""margin: 5px 0 0 0; color: #1e3a8a;"">Review the submitted report and provide approval or feedback.</p>
            </div>";

        return GetEmailTemplate(
            $"New Daily Report Submitted: {taskTitle}",
            content,
            "Review Update",
            taskUrl
        );
    }

    public static string GetDeadlineReminderTemplate(string fullName, string taskTitle, DateTime dueDate, string baseUrl)
    {
        var taskUrl = $"{baseUrl}/tasks";
        var daysUntilDue = (dueDate - DateTime.UtcNow).Days;
        var urgencyLevel = daysUntilDue <= 1 ? "high" : daysUntilDue <= 3 ? "medium" : "low";
        var urgencyColor = daysUntilDue <= 1 ? ErrorColor : daysUntilDue <= 3 ? WarningColor : PrimaryColor;
        var urgencyMessage = daysUntilDue <= 0 
            ? "is overdue!" 
            : daysUntilDue == 1 
                ? "is due tomorrow!" 
                : daysUntilDue <= 7
                    ? $"is due in {daysUntilDue} days."
                    : $"is due in {daysUntilDue} days.";

        var content = $@"
            <p>Hello {fullName},</p>
            <p>This is a reminder that the task <strong>{taskTitle}</strong> {urgencyMessage}</p>
            <div style=""padding: 20px; background: linear-gradient(135deg, {(daysUntilDue <= 1 ? "#fee2e2" : daysUntilDue <= 3 ? "#fef3c7" : "#eff6ff")} 0%, {(daysUntilDue <= 1 ? "#fecaca" : daysUntilDue <= 3 ? "#fde68a" : "#dbeafe")} 100%); border-radius: 8px; margin: 20px 0; border-left: 4px solid {urgencyColor};"">
                <div style=""display: flex; justify-content: space-between; align-items: center;"">
                    <div>
                        <p style=""margin: 0 0 5px 0; font-weight: 600; color: #111827;"">Due Date:</p>
                        <p style=""margin: 0; font-size: 18px; font-weight: 700; color: {urgencyColor};"">{dueDate:MMMM dd, yyyy}</p>
                    </div>
                    <div style=""font-size: 32px;"">⏰</div>
                </div>
            </div>
            <p>Please ensure you complete this task on time to keep the project on track.</p>";

        return GetEmailTemplate(
            $"Deadline Reminder: {taskTitle}",
            content,
            "View Task",
            taskUrl
        );
    }

    public static string GetDailyUpdateReminderTemplate(string fullName, int taskCount, string tasksList, string baseUrl)
    {
        var taskUrl = $"{baseUrl}/tasks";
        var taskWord = taskCount == 1 ? "task" : "tasks";
        
        var content = $@"
            <p>Hello {fullName},</p>
            <p>This is your daily reminder to submit progress updates for your assigned {taskWord}.</p>
            <div style=""padding: 20px; background: linear-gradient(135deg, #f0f9ff 0%, #e0f2fe 100%); border-radius: 8px; margin: 20px 0; border-left: 4px solid {PrimaryColor};"">
                <h3 style=""margin: 0 0 15px 0; color: {PrimaryColor}; font-size: 18px; font-weight: 700;"">Your Active Tasks ({taskCount})</h3>
                <p style=""margin: 0; color: #1e3a8a; line-height: 1.8;"">{tasksList}</p>
            </div>
            <div style=""padding: 15px; background-color: #f9fafb; border-radius: 6px; margin: 20px 0;"">
                <p style=""margin: 0 0 10px 0; font-weight: 600; color: #374151;"">Please take a moment to:</p>
                <ul style=""margin: 0; padding-left: 20px; color: #4b5563;"">
                    <li>Upload photos or videos of your progress</li>
                    <li>Describe the work completed today</li>
                    <li>Update the progress percentage</li>
                    <li>Note any challenges or blockers</li>
                </ul>
            </div>
            <p style=""color: #6b7280; font-size: 14px; margin-top: 20px;"">
                <strong>Note:</strong> This is an automated reminder sent on workdays at 6 PM. Submitting daily updates helps track project progress accurately.
            </p>";

        return GetEmailTemplate(
            "Daily Work Update Reminder",
            content,
            "Submit Update",
            taskUrl
        );
    }
}

