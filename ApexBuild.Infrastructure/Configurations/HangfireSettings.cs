namespace ApexBuild.Infrastructure.Configurations
{
    /// <summary>
    /// Hangfire configuration settings for background jobs.
    /// </summary>
    public class HangfireSettings
    {
        public bool IsEnabled { get; set; } = true;
        public string ConnectionString { get; set; } = string.Empty;
        public int DashboardAuthorizationLevel { get; set; } = 1; // 0 = public, 1 = requires auth
        public int JobExpirationDays { get; set; } = 30;
    }
}
