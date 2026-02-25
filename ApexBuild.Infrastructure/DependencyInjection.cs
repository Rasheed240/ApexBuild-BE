using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Infrastructure.BackgroundJobs;
using ApexBuild.Infrastructure.Configurations;
using ApexBuild.Infrastructure.Repositories;
using ApexBuild.Infrastructure.Services;
using CloudinaryDotNet;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApexBuild.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
        {
            // Register Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ── Cache ──────────────────────────────────────────────────────────────────
            // IMemoryCache is registered in Program.cs via AddMemoryCache().
            // CacheService wraps it with prefix-invalidation and TTL from config.
            services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));
            services.AddSingleton<ICacheService, CacheService>();

            // Register Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IOrganizationRepository, OrganizationRepository>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<ITaskUpdateRepository, TaskUpdateRepository>();
            services.AddScoped<IInvitationRepository, InvitationRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();

            // Register Services
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IPasswordHistoryService, PasswordHistoryService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<ITwoFactorService, TwoFactorService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<IStripePaymentService, StripePaymentService>(); // Using real Stripe service
            services.AddScoped<ISubscriptionBillingService, SubscriptionBillingService>();
            services.AddScoped<IPaymentProcessingService, PaymentProcessingService>();
            services.AddScoped<ILicenseNotificationService, LicenseNotificationService>();
            services.AddScoped<IBackgroundJobService, BackgroundJobService>();
            services.AddSingleton<IDateTimeService, DateTimeService>();

            // Configure Cloudinary
            var cloudinaryConfig = configuration.GetSection("Cloudinary");
            var cloudinaryAccount = new Account(
                cloudinaryConfig["CloudName"],
                cloudinaryConfig["ApiKey"],
                cloudinaryConfig["ApiSecret"]);

            services.AddSingleton(new Cloudinary(cloudinaryAccount));
            services.AddScoped<ICloudinaryService, CloudinaryService>();

            // Configure Stripe
            services.Configure<StripeSettings>(configuration.GetSection("StripeSettings"));

            // Configure Hangfire
            services.Configure<HangfireSettings>(configuration.GetSection("HangfireSettings"));
            
            // Register Hangfire services
            var hangfireSettings = configuration.GetSection("HangfireSettings").Get<HangfireSettings>();
            if (hangfireSettings?.IsEnabled == true && !string.IsNullOrEmpty(hangfireSettings.ConnectionString))
            {
                services.AddHangfire(config => config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UsePostgreSqlStorage(hangfireSettings.ConnectionString));

                services.AddHangfireServer();
            }

            // Register Background Jobs
            services.AddScoped<DeadlineReminderJob>();
            services.AddScoped<PendingApprovalReminderJob>();
            services.AddScoped<ExpiredInvitationCleanupJob>();

            // Register Database Seeders
            services.AddScoped<ApexBuild.Infrastructure.Data.DatabaseSeeder>();
            services.AddScoped<ApexBuild.Infrastructure.Data.DatabaseSeeder2>();

            return services;
        }
    }
}


