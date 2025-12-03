using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using Microsoft.Extensions.Logging;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Infrastructure.Data
{
    public class DatabaseSeeder
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<DatabaseSeeder> _logger;

        // Collections to store created entities for relationship building
        private readonly List<User> _users = new();
        private readonly List<Role> _roles = new();
        private readonly List<Organization> _organizations = new();
        private readonly List<Subscription> _subscriptions = new();
        private readonly List<Project> _projects = new();
        private readonly List<Department> _departments = new();
        private readonly List<ProjectTask> _tasks = new();

        public DatabaseSeeder(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            ISubscriptionService subscriptionService,
            ILogger<DatabaseSeeder> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting database seeding...");

                // Check if already seeded
                //var existingUser = await _unitOfWork.Users.GetByEmailAsync("usertest001@mailinator.com", cancellationToken);
                //if (existingUser != null)
                //{
                //    return (false, "Database has already been seeded. Use the clear endpoint first if you want to re-seed.");
                //}

                // Seed in dependency order
                await SeedRolesAsync(cancellationToken);
                await SeedUsersAsync(cancellationToken);
                await SeedOrganizationsAsync(cancellationToken);
                await SeedSubscriptionsAsync(cancellationToken);
                await SeedOrganizationMembersAndLicensesAsync(cancellationToken);
                await SeedProjectsAsync(cancellationToken);
                await SeedProjectUsersAndRolesAsync(cancellationToken);
                await SeedDepartmentsAsync(cancellationToken);
                await SeedWorkInfosAsync(cancellationToken);
                await SeedTasksAsync(cancellationToken);
                await SeedSubtasksAsync(cancellationToken);
                await SeedTaskUpdatesAndCommentsAsync(cancellationToken);
                await SeedInvitationsAsync(cancellationToken);
                await SeedNotificationsAsync(cancellationToken);

                _logger.LogInformation("Database seeding completed successfully!");
                return (true, $"Successfully seeded database with {_users.Count} users, {_organizations.Count} organizations, {_projects.Count} projects, {_tasks.Count} tasks, and all related entities.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during database seeding");
                return (false, $"Error during seeding: {ex.Message}");
            }
        }

        private async Task SeedRolesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Seeding roles...");

            var roleTypes = new[]
            {
                (RoleType.SuperAdmin, "Super Administrator", "Full system access", 1),
                (RoleType.PlatformAdmin, "Platform Administrator", "Platform-level administration", 2),
                (RoleType.ProjectOwner, "Project Owner", "Owns and controls projects", 3),
                (RoleType.ProjectAdministrator, "Project Administrator", "Manages projects", 4),
                (RoleType.ContractorAdmin, "Contractor Administrator", "Manages contractor organization", 5),
                (RoleType.DepartmentSupervisor, "Department Supervisor", "Supervises departments", 6),
                (RoleType.FieldWorker, "Field Worker", "Executes tasks", 7),
                (RoleType.Observer, "Observer", "Read-only access", 8)
            };

            foreach (var (roleType, name, description, level) in roleTypes)
            {
                var role = new Role
                {
                    Name = name,
                    RoleType = roleType,
                    Description = description,
                    IsSystemRole = true,
                    Level = level
                };
                _roles.Add(role);
                await _unitOfWork.Roles.AddAsync(role, cancellationToken);
            }

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during database seeding");

            }

            _logger.LogInformation($"Seeded {_roles.Count} roles");
        }

        private async Task SeedUsersAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Seeding users...");

            var passwordHash = _passwordHasher.HashPassword("Password123%");

            var userDataList = new[]
            {
                new { Email = "usertest001@mailinator.com", FirstName = "John", LastName = "Doe", Role = RoleType.SuperAdmin, Position = "CEO" },
                new { Email = "usertest002@mailinator.com", FirstName = "Jane", LastName = "Smith", Role = RoleType.PlatformAdmin, Position = "Platform Manager" },
                new { Email = "usertest003@mailinator.com", FirstName = "Michael", LastName = "Johnson", Role = RoleType.ProjectOwner, Position = "Project Owner" },
                new { Email = "usertest004@mailinator.com", FirstName = "Sarah", LastName = "Williams", Role = RoleType.ProjectAdministrator, Position = "Project Manager" },
                new { Email = "usertest005@mailinator.com", FirstName = "David", LastName = "Brown", Role = RoleType.DepartmentSupervisor, Position = "Electrical Supervisor" },
                new { Email = "usertest006@mailinator.com", FirstName = "Emily", LastName = "Davis", Role = RoleType.FieldWorker, Position = "Electrician" },
                new { Email = "usertest007@mailinator.com", FirstName = "Robert", LastName = "Miller", Role = RoleType.ContractorAdmin, Position = "Contractor CEO" },
                new { Email = "usertest008@mailinator.com", FirstName = "Lisa", LastName = "Wilson", Role = RoleType.DepartmentSupervisor, Position = "Plumbing Supervisor" },
                new { Email = "usertest009@mailinator.com", FirstName = "James", LastName = "Moore", Role = RoleType.FieldWorker, Position = "Plumber" },
                new { Email = "usertest010@mailinator.com", FirstName = "Jennifer", LastName = "Taylor", Role = RoleType.ProjectOwner, Position = "Owner" },
                new { Email = "usertest011@mailinator.com", FirstName = "William", LastName = "Anderson", Role = RoleType.DepartmentSupervisor, Position = "HVAC Supervisor" },
                new { Email = "usertest012@mailinator.com", FirstName = "Mary", LastName = "Thomas", Role = RoleType.Observer, Position = "Inspector" }
            };

            foreach (var userData in userDataList)
            {
                var user = new User
                {
                    Email = userData.Email,
                    FirstName = userData.FirstName,
                    LastName = userData.LastName,
                    PasswordHash = passwordHash,
                    EmailConfirmed = true,
                    EmailConfirmedAt = DateTime.UtcNow,
                    Status = UserStatus.Active,
                    PhoneNumber = $"+1-555-{Random.Shared.Next(100, 999)}-{Random.Shared.Next(1000, 9999)}",
                    City = "New York",
                    State = "NY",
                    Country = "USA",
                    Bio = $"{userData.Position} with extensive experience in construction management."
                };

                // Add role to user ONLY if it is a system role (SuperAdmin or PlatformAdmin)
                if (userData.Role == RoleType.SuperAdmin || userData.Role == RoleType.PlatformAdmin)
                {
                    var role = _roles.First(r => r.RoleType == userData.Role);
                    var userRole = new UserRole
                    {
                        User = user,
                        Role = role,
                        RoleId = role.Id,
                        IsActive = true,
                        ActivatedAt = DateTime.UtcNow,
                        OrganizationId = null // System roles are global
                    };
                    user.UserRoles.Add(userRole);
                    await _unitOfWork.UserRoles.AddAsync(userRole, cancellationToken);
                }

                _users.Add(user);
                await _unitOfWork.Users.AddAsync(user, cancellationToken);
            }

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during database seeding");

            }

            _logger.LogInformation($"Seeded {_users.Count} users");
        }

        private async Task SeedOrganizationsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Seeding organizations...");

            var organizationData = new[]
            {
                new { Name = "TechBuild Corp", Code = "TBC", OwnerEmail = "usertest001@mailinator.com", Description = "Leading construction technology company" },
                new { Name = "BuildPro Solutions", Code = "BPS", OwnerEmail = "usertest007@mailinator.com", Description = "Professional building contractors" },
                new { Name = "ConstructCo Ltd", Code = "CCL", OwnerEmail = "usertest010@mailinator.com", Description = "Industrial construction specialists" }
            };

            foreach (var orgData in organizationData)
            {
                var owner = _users.First(u => u.Email == orgData.OwnerEmail);
                var organization = new Organization
                {
                    Name = orgData.Name,
                    Code = orgData.Code,
                    Description = orgData.Description,
                    OwnerId = owner.Id,
                    Email = $"contact@{orgData.Code.ToLower()}.com",
                    PhoneNumber = $"+1-800-{Random.Shared.Next(100, 999)}-{Random.Shared.Next(1000, 9999)}",
                    City = "New York",
                    State = "NY",
                    Country = "USA",
                    IsActive = true,
                    IsVerified = true,
                    VerifiedAt = DateTime.UtcNow
                };

                _organizations.Add(organization);
                await _unitOfWork.Organizations.AddAsync(organization, cancellationToken);

                // Assign Owner Role scoped to this Organization
                // Find the role type intended for this user from the initial list (simplification: assuming Owner needs ContractorAdmin or ProjectOwner)
                // For simplicity in this seed, we'll assign 'ProjectOwner' or 'ContractorAdmin' based on context or just default to ContractorAdmin for org owners if not specified.
                // However, matching the original seed intent:
                // usertest001 (TechBuild) was SuperAdmin globally. He is also Owner of TechBuild. He might need an org-scoped role too if the system requires it.
                // usertest007 (BuildPro) was ContractorAdmin globally.
                // usertest010 (ConstructCo) was ProjectOwner globally. 
                
                RoleType ownerRoleType = RoleType.ContractorAdmin; // Default
                if (orgData.OwnerEmail == "usertest010@mailinator.com") ownerRoleType = RoleType.ProjectOwner; // Based on original intent
                
                var role = _roles.First(r => r.RoleType == ownerRoleType);
                var userRole = new UserRole
                {
                    UserId = owner.Id,
                    RoleId = role.Id,
                    OrganizationId = organization.Id,
                    IsActive = true,
                    ActivatedAt = DateTime.UtcNow
                };
                await _unitOfWork.UserRoles.AddAsync(userRole, cancellationToken);
            }
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during database seeding");

            }

            _logger.LogInformation($"Seeded {_organizations.Count} organizations");
        }

        private async Task SeedSubscriptionsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Seeding subscriptions...");

            var subscriptionData = new[]
            {
                new { OrgCode = "TBC", Licenses = 10, Billing = SubscriptionBillingCycle.Monthly },
                new { OrgCode = "BPS", Licenses = 5, Billing = SubscriptionBillingCycle.Monthly },
                new { OrgCode = "CCL", Licenses = 5, Billing = SubscriptionBillingCycle.Annual }
            };

            foreach (var subData in subscriptionData)
            {
                var organization = _organizations.First(o => o.Code == subData.OrgCode);

                var subscription = new Subscription
                {
                    OrganizationId = organization.Id,
                    UserId = organization.OwnerId,
                    NumberOfLicenses = subData.Licenses,
                    LicensesUsed = 0,
                    LicenseCostPerMonth = 10m,
                    Status = SubscriptionStatus.Active,
                    BillingCycle = subData.Billing,
                    BillingStartDate = DateTime.UtcNow.AddDays(-30),
                    BillingEndDate = DateTime.UtcNow.AddMonths(subData.Billing == SubscriptionBillingCycle.Monthly ? 1 : 12),
                    NextBillingDate = DateTime.UtcNow.AddMonths(subData.Billing == SubscriptionBillingCycle.Monthly ? 1 : 12),
                    AutoRenew = true,
                    IsTrialPeriod = false,
                    Amount = subData.Licenses * 10m
                };

                _subscriptions.Add(subscription);
                await _unitOfWork.Subscriptions.AddAsync(subscription, cancellationToken);
            }

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during database seeding");

            }

            _logger.LogInformation($"Seeded {_subscriptions.Count} subscriptions");
        }

        private async Task SeedOrganizationMembersAndLicensesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Seeding organization members and licenses...");

            // TechBuild Corp members (usertest001-006)
            // usertest001 is SuperAdmin(Global) + Owner(OrgRole from prev step). Here we add him as member. Position matches.
            await AddOrgMemberWithLicense("TBC", "usertest001@mailinator.com", "CEO", RoleType.ContractorAdmin, cancellationToken); // Already added as owner, but ensure consistency
            await AddOrgMemberWithLicense("TBC", "usertest002@mailinator.com", "Platform Manager", RoleType.PlatformAdmin, cancellationToken); // PlatformAdmin global, maybe also org admin?
            await AddOrgMemberWithLicense("TBC", "usertest003@mailinator.com", "Project Owner", RoleType.ProjectOwner, cancellationToken);
            await AddOrgMemberWithLicense("TBC", "usertest004@mailinator.com", "Project Manager", RoleType.ProjectAdministrator, cancellationToken);
            await AddOrgMemberWithLicense("TBC", "usertest005@mailinator.com", "Electrical Supervisor", RoleType.DepartmentSupervisor, cancellationToken);
            await AddOrgMemberWithLicense("TBC", "usertest006@mailinator.com", "Electrician", RoleType.FieldWorker, cancellationToken);

            // BuildPro Solutions members (usertest007-009)
            await AddOrgMemberWithLicense("BPS", "usertest007@mailinator.com", "Contractor CEO", RoleType.ContractorAdmin, cancellationToken);
            await AddOrgMemberWithLicense("BPS", "usertest008@mailinator.com", "Plumbing Supervisor", RoleType.DepartmentSupervisor, cancellationToken);
            await AddOrgMemberWithLicense("BPS", "usertest009@mailinator.com", "Plumber", RoleType.FieldWorker, cancellationToken);

            // ConstructCo Ltd members (usertest010-012)
            await AddOrgMemberWithLicense("CCL", "usertest010@mailinator.com", "Owner", RoleType.ProjectOwner, cancellationToken);
            await AddOrgMemberWithLicense("CCL", "usertest011@mailinator.com", "HVAC Supervisor", RoleType.DepartmentSupervisor, cancellationToken);
            await AddOrgMemberWithLicense("CCL", "usertest012@mailinator.com", "Inspector", RoleType.Observer, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded organization members and licenses");
        }

        private async Task AddOrgMemberWithLicense(string orgCode, string userEmail, string position, RoleType roleType, CancellationToken cancellationToken)
        {
            var organization = _organizations.First(o => o.Code == orgCode);
            var user = _users.First(u => u.Email == userEmail);
            var subscription = _subscriptions.First(s => s.OrganizationId == organization.Id);

            // Add organization member
            var member = new OrganizationMember
            {
                OrganizationId = organization.Id,
                UserId = user.Id,
                Position = position,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };
            await _unitOfWork.OrganizationMembers.AddAsync(member, cancellationToken);

            // Check if UserRole already exists for this Org (avoid duplicates from Owner assignment or repeated calls)
            var role = _roles.First(r => r.RoleType == roleType);
            var existingUserRole = await _unitOfWork.UserRoles.FindAsync(ur => 
                ur.UserId == user.Id && 
                ur.OrganizationId == organization.Id && 
                ur.RoleId == role.Id, cancellationToken);

            if (!existingUserRole.Any())
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    OrganizationId = organization.Id,
                    IsActive = true,
                    ActivatedAt = DateTime.UtcNow
                };
                await _unitOfWork.UserRoles.AddAsync(userRole, cancellationToken);
            }

            // Add license
            var license = new OrganizationLicense
            {
                OrganizationId = organization.Id,
                UserId = user.Id,
                SubscriptionId = subscription.Id,
                LicenseKey = $"LIC-{orgCode}-{user.Email.Split('@')[0].ToUpper()}",
                Status = LicenseStatus.Active,
                AssignedAt = DateTime.UtcNow,
                ValidFrom = DateTime.UtcNow,
                ValidUntil = subscription.BillingEndDate,
                LicenseType = "Full"
            };
            await _unitOfWork.OrganizationLicenses.AddAsync(license, cancellationToken);

            // Update subscription licenses used
            subscription.LicensesUsed++;
        }

        private async Task SeedProjectsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Seeding projects...");

            var projectData = new[]
            {
                new { Name = "Commercial Office Building", Code = "PROJ-2025-001", OrgCode = "TBC", OwnerEmail = "usertest003@mailinator.com", AdminEmail = "usertest004@mailinator.com", Status = ProjectStatus.Active, Type = "Commercial Building" },
                new { Name = "Shopping Mall Renovation", Code = "PROJ-2025-002", OrgCode = "TBC", OwnerEmail = "usertest003@mailinator.com", AdminEmail = "usertest004@mailinator.com", Status = ProjectStatus.Active, Type = "Renovation" },
                new { Name = "Residential Complex", Code = "PROJ-2025-003", OrgCode = "BPS", OwnerEmail = "usertest007@mailinator.com", AdminEmail = "usertest007@mailinator.com", Status = ProjectStatus.Planning, Type = "Residential" },
                new { Name = "Highway Bridge", Code = "PROJ-2025-004", OrgCode = "CCL", OwnerEmail = "usertest010@mailinator.com", AdminEmail = "usertest010@mailinator.com", Status = ProjectStatus.Active, Type = "Infrastructure" },
                new { Name = "School Building", Code = "PROJ-2025-005", OrgCode = "TBC", OwnerEmail = "usertest003@mailinator.com", AdminEmail = "usertest004@mailinator.com", Status = ProjectStatus.Completed, Type = "Educational" },
                new { Name = "Hospital Wing", Code = "PROJ-2025-006", OrgCode = "BPS", OwnerEmail = "usertest007@mailinator.com", AdminEmail = "usertest007@mailinator.com", Status = ProjectStatus.OnHold, Type = "Healthcare" }
            };

            // Sample project images
            var projectImages = new List<string>
            {
                "https://images.unsplash.com/photo-1486406146926-c627a92ad1ab?w=1200",
                "https://images.unsplash.com/photo-1503387762-592deb58ef4e?w=1200",
                "https://images.unsplash.com/photo-1541888946425-d81bb19240f5?w=1200",
                "https://images.unsplash.com/photo-1504307651254-35680f356dfd?w=1200",
                "https://images.unsplash.com/photo-1589939705384-5185137a7f0f?w=1200",
                "https://images.unsplash.com/photo-1590496793907-4c5de0f2e254?w=1200"
            };

            foreach (var projData in projectData)
            {
                var owner = _users.First(u => u.Email == projData.OwnerEmail);
                var admin = _users.First(u => u.Email == projData.AdminEmail);

                var org = _organizations.First(o => o.Code == projData.OrgCode);

                var project = new Project
                {
                    Name = projData.Name,
                    Code = projData.Code,
                    Description = $"{projData.Type} project with comprehensive scope and requirements.",
                    Status = projData.Status,
                    ProjectType = projData.Type,
                    Location = "New York, USA",
                    StartDate = DateTime.UtcNow.AddDays(-60),
                    ExpectedEndDate = DateTime.UtcNow.AddMonths(6),
                    Budget = Random.Shared.Next(500000, 5000000),
                    Currency = "USD",
                    OrganizationId = org.Id,
                    ProjectOwnerId = owner.Id,
                    ProjectAdminId = admin.Id,
                    IsActive = projData.Status != ProjectStatus.Cancelled,
                    ImageUrls = new List<string> { projectImages[Random.Shared.Next(projectImages.Count)] }
                };

                _projects.Add(project);
                await _unitOfWork.Projects.AddAsync(project, cancellationToken);
            }


            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during database seeding");

            }
            
            _logger.LogInformation($"Seeded {_projects.Count} projects");
        }

        private async Task SeedProjectUsersAndRolesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Seeding project users and roles...");

            // Commercial Office Building - Add team members
            await AddProjectUser("PROJ-2025-001", "usertest003@mailinator.com", RoleType.ProjectOwner, cancellationToken);
            await AddProjectUser("PROJ-2025-001", "usertest004@mailinator.com", RoleType.ProjectAdministrator, cancellationToken);
            await AddProjectUser("PROJ-2025-001", "usertest005@mailinator.com", RoleType.DepartmentSupervisor, cancellationToken);
            await AddProjectUser("PROJ-2025-001", "usertest006@mailinator.com", RoleType.FieldWorker, cancellationToken);

            // Shopping Mall Renovation
            await AddProjectUser("PROJ-2025-002", "usertest003@mailinator.com", RoleType.ProjectOwner, cancellationToken);
            await AddProjectUser("PROJ-2025-002", "usertest004@mailinator.com", RoleType.ProjectAdministrator, cancellationToken);

            // Residential Complex
            await AddProjectUser("PROJ-2025-003", "usertest007@mailinator.com", RoleType.ProjectOwner, cancellationToken);
            await AddProjectUser("PROJ-2025-003", "usertest008@mailinator.com", RoleType.DepartmentSupervisor, cancellationToken);
            await AddProjectUser("PROJ-2025-003", "usertest009@mailinator.com", RoleType.FieldWorker, cancellationToken);

            // Highway Bridge
            await AddProjectUser("PROJ-2025-004", "usertest010@mailinator.com", RoleType.ProjectOwner, cancellationToken);
            await AddProjectUser("PROJ-2025-004", "usertest011@mailinator.com", RoleType.DepartmentSupervisor, cancellationToken);
            await AddProjectUser("PROJ-2025-004", "usertest012@mailinator.com", RoleType.Observer, cancellationToken);

            // School Building
            await AddProjectUser("PROJ-2025-005", "usertest003@mailinator.com", RoleType.ProjectOwner, cancellationToken);
            await AddProjectUser("PROJ-2025-005", "usertest004@mailinator.com", RoleType.ProjectAdministrator, cancellationToken);

            // Hospital Wing
            await AddProjectUser("PROJ-2025-006", "usertest007@mailinator.com", RoleType.ProjectOwner, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded project users and roles");
        }

        private async Task AddProjectUser(string projectCode, string userEmail, RoleType roleType, CancellationToken cancellationToken)
        {
            var project = _projects.First(p => p.Code == projectCode);
            var user = _users.First(u => u.Email == userEmail);
            var role = _roles.First(r => r.RoleType == roleType);

            // Add ProjectUser
            var projectUser = new ProjectUser
            {
                ProjectId = project.Id,
                UserId = user.Id,
                RoleId = role.Id,
                Status = ProjectUserStatus.Active,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            await _unitOfWork.ProjectUsers.AddAsync(projectUser, cancellationToken);

            // Check if UserRole already exists for this combination
            var existingUserRole = user.UserRoles.FirstOrDefault(ur =>
                ur.RoleId == role.Id &&
                ur.ProjectId == project.Id);

            if (existingUserRole == null)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    ProjectId = project.Id,
                    OrganizationId = project.OrganizationId, // Ensure Project Roles are also linked to the Organization
                    IsActive = true,
                    ActivatedAt = DateTime.UtcNow
                };
                await _unitOfWork.UserRoles.AddAsync(userRole, cancellationToken);
            }
        }

        private async Task SeedDepartmentsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Seeding departments...");

            // Commercial Office Building departments
            await AddDepartment("PROJ-2025-001", "Electrical", "DEPT-ELE-001", "usertest005@mailinator.com", "Electrical Systems", cancellationToken);
            await AddDepartment("PROJ-2025-001", "Plumbing", "DEPT-PLU-001", "usertest008@mailinator.com", "Plumbing Systems", cancellationToken);
            await AddDepartment("PROJ-2025-001", "HVAC", "DEPT-HVAC-001", "usertest011@mailinator.com", "HVAC Systems", cancellationToken);

            // Shopping Mall Renovation
            await AddDepartment("PROJ-2025-002", "Structural", "DEPT-STR-001", "usertest005@mailinator.com", "Structural Engineering", cancellationToken);
            await AddDepartment("PROJ-2025-002", "Finishing", "DEPT-FIN-001", "usertest008@mailinator.com", "Interior Finishing", cancellationToken);

            // Residential Complex
            await AddDepartment("PROJ-2025-003", "Foundation", "DEPT-FND-001", "usertest008@mailinator.com", "Foundation Work", cancellationToken);
            await AddDepartment("PROJ-2025-003", "Framing", "DEPT-FRM-001", "usertest009@mailinator.com", "Framing and Structure", cancellationToken);

            // Highway Bridge
            await AddDepartment("PROJ-2025-004", "Concrete", "DEPT-CON-001", "usertest011@mailinator.com", "Concrete Work", cancellationToken);
            await AddDepartment("PROJ-2025-004", "Steel", "DEPT-STL-001", "usertest011@mailinator.com", "Steel Structure", cancellationToken);

            // School Building
            await AddDepartment("PROJ-2025-005", "Interior", "DEPT-INT-001", "usertest005@mailinator.com", "Interior Work", cancellationToken);
            await AddDepartment("PROJ-2025-005", "Exterior", "DEPT-EXT-001", "usertest005@mailinator.com", "Exterior Work", cancellationToken);

            // Hospital Wing
            await AddDepartment("PROJ-2025-006", "Medical Systems", "DEPT-MED-001", "usertest008@mailinator.com", "Medical Equipment Installation", cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"Seeded {_departments.Count} departments");
        }

        private async Task AddDepartment(string projectCode, string name, string code, string supervisorEmail, string specialization, CancellationToken cancellationToken)
        {
            var project = _projects.First(p => p.Code == projectCode);
            var supervisor = _users.First(u => u.Email == supervisorEmail);

            var department = new Department
            {
                Name = name,
                Code = code,
                Description = $"{specialization} department for {project.Name}",
                ProjectId = project.Id,
                OrganizationId = project.OrganizationId,
                SupervisorId = supervisor.Id,
                Status = DepartmentStatus.Active,
                StartDate = DateTime.UtcNow.AddDays(-30),
                IsActive = true,
                IsOutsourced = false,
                Specialization = specialization
            };

            _departments.Add(department);
            await _unitOfWork.Departments.AddAsync(department, cancellationToken);
        }

        private async Task SeedWorkInfosAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Seeding work info records...");

            // Add work info for key team members
            await AddWorkInfo("PROJ-2025-001", "DEPT-ELE-001", "usertest005@mailinator.com", "Electrical Supervisor", "EMP-TBC-005", 75m, cancellationToken);
            await AddWorkInfo("PROJ-2025-001", "DEPT-ELE-001", "usertest006@mailinator.com", "Electrician", "EMP-TBC-006", 45m, cancellationToken);

            await AddWorkInfo("PROJ-2025-003", "DEPT-FND-001", "usertest008@mailinator.com", "Plumbing Supervisor", "EMP-BPS-008", 70m, cancellationToken);
            await AddWorkInfo("PROJ-2025-003", "DEPT-FRM-001", "usertest009@mailinator.com", "Plumber", "EMP-BPS-009", 42m, cancellationToken);

            await AddWorkInfo("PROJ-2025-004", "DEPT-CON-001", "usertest011@mailinator.com", "HVAC Supervisor", "EMP-CCL-011", 72m, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded work info records");
        }


        private async Task AddWorkInfo(string projectCode, string deptCode, string userEmail, string position, string employeeId, decimal hourlyRate, CancellationToken cancellationToken)
        {
            var project = _projects.First(p => p.Code == projectCode);
            var department = _departments.First(d => d.Code == deptCode);
            var user = _users.First(u => u.Email == userEmail);

            // FIX: Use the project's OrganizationId directly instead of trying to find by owner
            var org = _organizations.First(o => o.Id == project.OrganizationId);

            var workInfo = new WorkInfo
            {
                UserId = user.Id,
                ProjectId = project.Id,
                OrganizationId = org.Id,
                DepartmentId = department.Id,
                Position = position,
                EmployeeId = employeeId,
                StartDate = DateTime.UtcNow.AddDays(-60),
                Status = ProjectUserStatus.Active,
                IsActive = true,
                Responsibilities = $"Responsible for {position.ToLower()} duties and team coordination.",
                HourlyRate = hourlyRate,
                ContractType = "FullTime"
            };

            await _unitOfWork.WorkInfos.AddAsync(workInfo, cancellationToken);
        }

        private async Task SeedTasksAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Seeding tasks...");

            // Commercial Office Building - Electrical Department
            await AddTask("DEPT-ELE-001", "Install Main Electrical Panel", "TASK-2025-001", "usertest006@mailinator.com", "usertest005@mailinator.com",
                TaskStatus.Completed, 4, 40, cancellationToken);
            await AddTask("DEPT-ELE-001", "Wire Floor 1-3", "TASK-2025-002", "usertest006@mailinator.com", "usertest005@mailinator.com",
                TaskStatus.InProgress, 3, 80, cancellationToken);
            await AddTask("DEPT-ELE-001", "Install Lighting Fixtures", "TASK-2025-003", "usertest006@mailinator.com", "usertest005@mailinator.com",
                TaskStatus.NotStarted, 2, 60, cancellationToken);

            // Commercial Office Building - Plumbing Department
            await AddTask("DEPT-PLU-001", "Install Water Supply Lines", "TASK-2025-004", "usertest009@mailinator.com", "usertest008@mailinator.com",
                TaskStatus.InProgress, 4, 50, cancellationToken);
            await AddTask("DEPT-PLU-001", "Install Drainage System", "TASK-2025-005", "usertest009@mailinator.com", "usertest008@mailinator.com",
                TaskStatus.NotStarted, 3, 45, cancellationToken);

            // Commercial Office Building - HVAC Department
            await AddTask("DEPT-HVAC-001", "Install HVAC Units", "TASK-2025-006", "usertest011@mailinator.com", "usertest011@mailinator.com",
                TaskStatus.UnderReview, 4, 70, cancellationToken);

            // Shopping Mall Renovation - Structural
            await AddTask("DEPT-STR-001", "Reinforce Main Columns", "TASK-2025-007", "usertest006@mailinator.com", "usertest005@mailinator.com",
                TaskStatus.Completed, 4, 100, cancellationToken);
            await AddTask("DEPT-STR-001", "Install Steel Beams", "TASK-2025-008", "usertest006@mailinator.com", "usertest005@mailinator.com",
                TaskStatus.InProgress, 4, 80, cancellationToken);

            // Shopping Mall Renovation - Finishing
            await AddTask("DEPT-FIN-001", "Drywall Installation", "TASK-2025-009", "usertest009@mailinator.com", "usertest008@mailinator.com",
                TaskStatus.NotStarted, 2, 60, cancellationToken);

            // Residential Complex - Foundation
            await AddTask("DEPT-FND-001", "Pour Foundation Concrete", "TASK-2025-010", "usertest009@mailinator.com", "usertest008@mailinator.com",
                TaskStatus.Completed, 4, 120, cancellationToken);

            // Residential Complex - Framing
            await AddTask("DEPT-FRM-001", "Frame Buildings 1-3", "TASK-2025-011", "usertest009@mailinator.com", "usertest009@mailinator.com",
                TaskStatus.InProgress, 3, 150, cancellationToken);

            // Highway Bridge - Concrete
            await AddTask("DEPT-CON-001", "Pour Bridge Deck", "TASK-2025-012", "usertest011@mailinator.com", "usertest011@mailinator.com",
                TaskStatus.UnderReview, 4, 200, cancellationToken);

            // Highway Bridge - Steel
            await AddTask("DEPT-STL-001", "Install Bridge Supports", "TASK-2025-013", "usertest011@mailinator.com", "usertest011@mailinator.com",
                TaskStatus.InProgress, 4, 180, cancellationToken);

            // Additional tasks for more variety
            // Commercial Office Building - Electrical Department (more tasks)
            await AddTask("DEPT-ELE-001", "Install Emergency Generators", "TASK-2025-014", "usertest006@mailinator.com", "usertest005@mailinator.com",
                TaskStatus.NotStarted, 4, 50, cancellationToken);
            await AddTask("DEPT-ELE-001", "Install Security Systems", "TASK-2025-015", "usertest006@mailinator.com", "usertest005@mailinator.com",
                TaskStatus.Pending, 3, 40, cancellationToken);

            // Shopping Mall Renovation - HVAC
            await AddTask("DEPT-HVAC-001", "Install Air Conditioning Units", "TASK-2025-016", "usertest011@mailinator.com", "usertest011@mailinator.com",
                TaskStatus.InProgress, 3, 80, cancellationToken);
            await AddTask("DEPT-HVAC-001", "Install Ventilation System", "TASK-2025-017", "usertest011@mailinator.com", "usertest011@mailinator.com",
                TaskStatus.NotStarted, 3, 60, cancellationToken);

            // Residential Complex - Electrical
            await AddTask("DEPT-ELE-001", "Install Smart Home Systems", "TASK-2025-018", "usertest006@mailinator.com", "usertest005@mailinator.com",
                TaskStatus.Pending, 2, 35, cancellationToken);

            // Shopping Mall - Finishing (more tasks)
            await AddTask("DEPT-FIN-001", "Paint Interior Walls", "TASK-2025-019", "usertest009@mailinator.com", "usertest008@mailinator.com",
                TaskStatus.NotStarted, 2, 50, cancellationToken);
            await AddTask("DEPT-FIN-001", "Install Flooring", "TASK-2025-020", "usertest009@mailinator.com", "usertest008@mailinator.com",
                TaskStatus.Pending, 3, 70, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"Seeded {_tasks.Count} parent tasks");
        }

        private async Task AddTask(string deptCode, string title, string code, string assignedToEmail, string assignedByEmail,
            TaskStatus status, int priority, int estimatedHours, CancellationToken cancellationToken)
        {
            var department = _departments.First(d => d.Code == deptCode);
            var assignedTo = _users.First(u => u.Email == assignedToEmail);
            var assignedBy = _users.First(u => u.Email == assignedByEmail);

            // Sample construction/building images from Unsplash
            var sampleImages = new List<string>
            {
                "https://images.unsplash.com/photo-1541888946425-d81bb19240f5?w=800",
                "https://images.unsplash.com/photo-1503387762-592deb58ef4e?w=800",
                "https://images.unsplash.com/photo-1504307651254-35680f356dfd?w=800",
                "https://images.unsplash.com/photo-1589939705384-5185137a7f0f?w=800",
                "https://images.unsplash.com/photo-1572981779307-38b8cabb2407?w=800",
                "https://images.unsplash.com/photo-1590496793907-4c5de0f2e254?w=800"
            };

            var task = new ProjectTask
            {
                Title = title,
                Description = $"Complete {title.ToLower()} according to project specifications and safety standards.",
                Code = code,
                ProjectId = department.ProjectId,
                DepartmentId = department.Id,
                AssignedByUserId = assignedBy.Id,
                Status = status,
                Priority = priority,
                StartDate = DateTime.UtcNow.AddDays(-20),
                DueDate = DateTime.UtcNow.AddDays(30),
                CompletedAt = status == TaskStatus.Completed ? DateTime.UtcNow.AddDays(-5) : null,
                EstimatedHours = estimatedHours,
                ActualHours = status == TaskStatus.Completed ? estimatedHours + Random.Shared.Next(-5, 10) : null,
                Progress = status switch
                {
                    TaskStatus.NotStarted => 0,
                    TaskStatus.InProgress => Random.Shared.Next(30, 70),
                    TaskStatus.UnderReview => Random.Shared.Next(75, 95),
                    TaskStatus.Completed => 100,
                    _ => 0
                },
                ImageUrls = new List<string> { sampleImages[Random.Shared.Next(sampleImages.Count)] },
                Tags = new List<string> { "Construction", "2025", department.Name.Split(' ')[0] }
            };

            _tasks.Add(task);
            await _unitOfWork.Tasks.AddAsync(task, cancellationToken);

            // Add task-user assignment (primary assignee)
            var taskUser = new TaskUser
            {
                TaskId = task.Id,
                UserId = assignedTo.Id,
                AssignedByUserId = assignedBy.Id,
                AssignedAt = DateTime.UtcNow,
                IsActive = true,
                Role = "Primary"
            };
            await _unitOfWork.TaskUsers.AddAsync(taskUser, cancellationToken);
        }

        private async Task SeedSubtasksAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Seeding subtasks...");

            // Wire Floor 1-3 (TASK-2025-002)
            await AddSubtask("TASK-2025-002", "Wire Floor 1", "TASK-2025-002-1", "usertest006@mailinator.com",
                TaskStatus.Completed, 3, 25, cancellationToken);
            await AddSubtask("TASK-2025-002", "Wire Floor 2", "TASK-2025-002-2", "usertest006@mailinator.com",
                TaskStatus.InProgress, 3, 25, cancellationToken);
            await AddSubtask("TASK-2025-002", "Wire Floor 3", "TASK-2025-002-3", "usertest006@mailinator.com",
                TaskStatus.NotStarted, 3, 30, cancellationToken);

            // Install Lighting Fixtures (TASK-2025-003)
            await AddSubtask("TASK-2025-003", "Install LED Panel Lights", "TASK-2025-003-1", "usertest006@mailinator.com",
                TaskStatus.NotStarted, 2, 20, cancellationToken);
            await AddSubtask("TASK-2025-003", "Install Emergency Lighting", "TASK-2025-003-2", "usertest006@mailinator.com",
                TaskStatus.NotStarted, 3, 20, cancellationToken);
            await AddSubtask("TASK-2025-003", "Install Outdoor Lighting", "TASK-2025-003-3", "usertest006@mailinator.com",
                TaskStatus.NotStarted, 2, 20, cancellationToken);

            // Install Water Supply Lines (TASK-2025-004)
            await AddSubtask("TASK-2025-004", "Connect Main Water Line", "TASK-2025-004-1", "usertest009@mailinator.com",
                TaskStatus.Completed, 4, 20, cancellationToken);
            await AddSubtask("TASK-2025-004", "Install Floor Supply Lines", "TASK-2025-004-2", "usertest009@mailinator.com",
                TaskStatus.InProgress, 3, 30, cancellationToken);

            // Install Drainage System (TASK-2025-005)
            await AddSubtask("TASK-2025-005", "Install Main Drainage Pipes", "TASK-2025-005-1", "usertest009@mailinator.com",
                TaskStatus.NotStarted, 3, 25, cancellationToken);
            await AddSubtask("TASK-2025-005", "Install Floor Drains", "TASK-2025-005-2", "usertest009@mailinator.com",
                TaskStatus.NotStarted, 3, 20, cancellationToken);

            // Install HVAC Units (TASK-2025-006)
            await AddSubtask("TASK-2025-006", "Install Rooftop Units", "TASK-2025-006-1", "usertest011@mailinator.com",
                TaskStatus.InProgress, 4, 35, cancellationToken);
            await AddSubtask("TASK-2025-006", "Install Ductwork", "TASK-2025-006-2", "usertest011@mailinator.com",
                TaskStatus.InProgress, 3, 35, cancellationToken);

            // Install Steel Beams (TASK-2025-008)
            await AddSubtask("TASK-2025-008", "Inspect Beam Specifications", "TASK-2025-008-1", "usertest006@mailinator.com",
                TaskStatus.Completed, 2, 10, cancellationToken);
            await AddSubtask("TASK-2025-008", "Install North Wing Beams", "TASK-2025-008-2", "usertest006@mailinator.com",
                TaskStatus.InProgress, 4, 40, cancellationToken);
            await AddSubtask("TASK-2025-008", "Install South Wing Beams", "TASK-2025-008-3", "usertest006@mailinator.com",
                TaskStatus.NotStarted, 4, 30, cancellationToken);

            // Drywall Installation (TASK-2025-009)
            await AddSubtask("TASK-2025-009", "Install Drywall - First Floor", "TASK-2025-009-1", "usertest009@mailinator.com",
                TaskStatus.NotStarted, 2, 30, cancellationToken);
            await AddSubtask("TASK-2025-009", "Install Drywall - Second Floor", "TASK-2025-009-2", "usertest009@mailinator.com",
                TaskStatus.NotStarted, 2, 30, cancellationToken);

            // Frame Buildings 1-3 (TASK-2025-011)
            await AddSubtask("TASK-2025-011", "Frame Building 1", "TASK-2025-011-1", "usertest009@mailinator.com",
                TaskStatus.Completed, 3, 50, cancellationToken);
            await AddSubtask("TASK-2025-011", "Frame Building 2", "TASK-2025-011-2", "usertest009@mailinator.com",
                TaskStatus.InProgress, 3, 50, cancellationToken);
            await AddSubtask("TASK-2025-011", "Frame Building 3", "TASK-2025-011-3", "usertest009@mailinator.com",
                TaskStatus.NotStarted, 3, 50, cancellationToken);

            // Install Bridge Supports (TASK-2025-013)
            await AddSubtask("TASK-2025-013", "Install North Support Columns", "TASK-2025-013-1", "usertest011@mailinator.com",
                TaskStatus.InProgress, 4, 90, cancellationToken);
            await AddSubtask("TASK-2025-013", "Install South Support Columns", "TASK-2025-013-2", "usertest011@mailinator.com",
                TaskStatus.NotStarted, 4, 90, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded subtasks");

            // Add additional assignees to some tasks to demonstrate multiple assignees
            await AddAdditionalAssignees(cancellationToken);
        }

        private async Task AddAdditionalAssignees(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Adding additional assignees to tasks...");

            // TASK-2025-002: Wire Floor 1-3 - Add a support electrician
            await AddTaskAssignee("TASK-2025-002", "usertest007@mailinator.com", "usertest005@mailinator.com", "Support", cancellationToken);

            // TASK-2025-004: Install Water Supply Lines - Add a helper
            await AddTaskAssignee("TASK-2025-004", "usertest010@mailinator.com", "usertest008@mailinator.com", "Assistant", cancellationToken);

            // TASK-2025-006: Install HVAC Units - Add technician and supervisor
            await AddTaskAssignee("TASK-2025-006", "usertest012@mailinator.com", "usertest011@mailinator.com", "Technician", cancellationToken);
            await AddTaskAssignee("TASK-2025-006", "usertest010@mailinator.com", "usertest011@mailinator.com", "Supervisor", cancellationToken);

            // TASK-2025-008: Install Steel Beams - Add safety officer
            await AddTaskAssignee("TASK-2025-008", "usertest007@mailinator.com", "usertest005@mailinator.com", "Safety Officer", cancellationToken);

            // TASK-2025-011: Frame Buildings 1-3 - Add multiple workers
            await AddTaskAssignee("TASK-2025-011", "usertest010@mailinator.com", "usertest009@mailinator.com", "Lead Carpenter", cancellationToken);
            await AddTaskAssignee("TASK-2025-011", "usertest011@mailinator.com", "usertest009@mailinator.com", "Carpenter", cancellationToken);

            // TASK-2025-013: Install Bridge Supports - Add QA inspector
            await AddTaskAssignee("TASK-2025-013", "usertest012@mailinator.com", "usertest011@mailinator.com", "QA Inspector", cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Added additional assignees to tasks");
        }

        private async Task AddTaskAssignee(string taskCode, string assigneeEmail, string assignedByEmail, string role, CancellationToken cancellationToken)
        {
            var task = _tasks.First(t => t.Code == taskCode);
            var assignee = _users.First(u => u.Email == assigneeEmail);
            var assignedBy = _users.First(u => u.Email == assignedByEmail);

            var taskUser = new TaskUser
            {
                TaskId = task.Id,
                UserId = assignee.Id,
                AssignedByUserId = assignedBy.Id,
                AssignedAt = DateTime.UtcNow,
                IsActive = true,
                Role = role
            };

            await _unitOfWork.TaskUsers.AddAsync(taskUser, cancellationToken);
        }

        private async Task AddSubtask(string parentTaskCode, string title, string code, string assignedToEmail,
            TaskStatus status, int priority, int estimatedHours, CancellationToken cancellationToken)
        {
            var parentTask = _tasks.First(t => t.Code == parentTaskCode);
            var assignedTo = _users.First(u => u.Email == assignedToEmail);

            // Sample construction/work progress images
            var sampleImages = new List<string>
            {
                "https://images.unsplash.com/photo-1504307651254-35680f356dfd?w=600",
                "https://images.unsplash.com/photo-1581094271901-8022df4466f9?w=600",
                "https://images.unsplash.com/photo-1572981779307-38b8cabb2407?w=600",
                "https://images.unsplash.com/photo-1590496793907-4c5de0f2e254?w=600"
            };

            var subtask = new ProjectTask
            {
                Title = title,
                Description = $"Subtask: {title}",
                Code = code,
                ProjectId = parentTask.ProjectId,
                DepartmentId = parentTask.DepartmentId,
                ParentTaskId = parentTask.Id,
                AssignedByUserId = parentTask.AssignedByUserId,
                Status = status,
                Priority = priority,
                StartDate = DateTime.UtcNow.AddDays(-15),
                DueDate = DateTime.UtcNow.AddDays(20),
                CompletedAt = status == TaskStatus.Completed ? DateTime.UtcNow.AddDays(-3) : null,
                EstimatedHours = estimatedHours,
                Progress = status switch
                {
                    TaskStatus.NotStarted => 0,
                    TaskStatus.InProgress => Random.Shared.Next(40, 80),
                    TaskStatus.Completed => 100,
                    _ => 0
                },
                ImageUrls = new List<string> { sampleImages[Random.Shared.Next(sampleImages.Count)] },
                Tags = new List<string> { "Subtask", "2025" }
            };

            await _unitOfWork.Tasks.AddAsync(subtask, cancellationToken);

            // Add task-user assignment for subtask
            var taskUser = new TaskUser
            {
                TaskId = subtask.Id,
                UserId = assignedTo.Id,
                AssignedByUserId = parentTask.AssignedByUserId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true,
                Role = "Primary"
            };
            await _unitOfWork.TaskUsers.AddAsync(taskUser, cancellationToken);
        }

        private async Task SeedTaskUpdatesAndCommentsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Seeding task updates and comments...");

            // Add updates for some in-progress tasks
            await AddTaskUpdateWithComment("TASK-2025-002", "usertest006@mailinator.com", "usertest005@mailinator.com",
                "Completed wiring for Floor 1 and 2. Starting Floor 3 tomorrow.", 65, true, cancellationToken);

            await AddTaskUpdateWithComment("TASK-2025-004", "usertest009@mailinator.com", "usertest008@mailinator.com",
                "Main water line connected. Installing floor supply lines in progress.", 60, true, cancellationToken);

            await AddTaskUpdateWithComment("TASK-2025-006", "usertest011@mailinator.com", "usertest011@mailinator.com",
                "All HVAC units installed and tested. Ready for final inspection.", 95, false, cancellationToken);

            await AddTaskUpdateWithComment("TASK-2025-008", "usertest006@mailinator.com", "usertest005@mailinator.com",
                "North wing beams installation 80% complete. Awaiting final beam delivery.", 75, true, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded task updates and comments");
        }

        private async Task AddTaskUpdateWithComment(string taskCode, string submittedByEmail, string supervisorEmail,
            string description, decimal progress, bool supervisorApproved, CancellationToken cancellationToken)
        {
            var task = _tasks.First(t => t.Code == taskCode);
            var submittedBy = _users.First(u => u.Email == submittedByEmail);
            var supervisor = _users.First(u => u.Email == supervisorEmail);

            // Add task update
            var update = new TaskUpdate
            {
                TaskId = task.Id,
                SubmittedByUserId = submittedBy.Id,
                Description = description,
                Status = supervisorApproved ? UpdateStatus.UnderAdminReview : UpdateStatus.UnderSupervisorReview,
                ProgressPercentage = progress,
                Summary = $"Progress update: {progress}% complete",
                SubmittedAt = DateTime.UtcNow.AddDays(-2),
                ReviewedBySupervisorId = supervisorApproved ? supervisor.Id : null,
                SupervisorReviewedAt = supervisorApproved ? DateTime.UtcNow.AddDays(-1) : null,
                SupervisorFeedback = supervisorApproved ? "Good progress. Approved for admin review." : null,
                SupervisorApproved = supervisorApproved ? true : null
            };
            await _unitOfWork.TaskUpdates.AddAsync(update, cancellationToken);

            // Add task comment
            var comment = new TaskComment
            {
                TaskId = task.Id,
                UserId = supervisor.Id,
                Comment = supervisorApproved
                    ? "Great work! Keep up the good progress."
                    : "Please review the specifications before proceeding further."
            };
            await _unitOfWork.TaskComments.AddAsync(comment, cancellationToken);
        }

        private async Task SeedInvitationsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Seeding invitations...");

            var project = _projects.First(p => p.Code == "PROJ-2025-001");
            var inviter = _users.First(u => u.Email == "usertest004@mailinator.com");
            var role = _roles.First(r => r.RoleType == RoleType.FieldWorker);

            var invitation = new Invitation
            {
                Email = "newuser@mailinator.com",
                Token = Guid.NewGuid().ToString(),
                InvitedByUserId = inviter.Id,
                RoleId = role.Id,
                ProjectId = project.Id,
                OrganizationId = project.OrganizationId, // FIX: Add organization context
                Status = InvitationStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Message = "You are invited to join the Commercial Office Building project as a Field Worker."
            };

            await _unitOfWork.Invitations.AddAsync(invitation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded invitations");
        }

        private async Task SeedNotificationsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Seeding notifications...");

            // Add sample notifications for users
            await AddNotification("usertest004@mailinator.com", "Task Update Review",
                "New task update awaiting your review for task TASK-2025-006", NotificationType.TaskUpdate, cancellationToken);

            await AddNotification("usertest005@mailinator.com", "Task Assigned",
                "You have been assigned a new task: Install Lighting Fixtures", NotificationType.TaskAssigned, cancellationToken);

            await AddNotification("usertest001@mailinator.com", "License Expiring Soon",
                "Organization license for TechBuild Corp will expire in 30 days", NotificationType.DeadlineReminder, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded notifications");
        }

        private async Task AddNotification(string userEmail, string title, string message, NotificationType type, CancellationToken cancellationToken)
        {
            var user = _users.First(u => u.Email == userEmail);

            var notification = new Notification
            {
                UserId = user.Id,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                Channel = NotificationChannel.InApp
            };

            await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        }

        public async Task<(bool Success, string Message)> ClearSeededDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Clearing seeded data...");

                // Soft delete all test users and their related data will cascade
                var testUsers = await _unitOfWork.Users.FindAsync(
                    u => u.Email.StartsWith("usertest") && u.Email.Contains("@mailinator.com"),
                    cancellationToken);

                foreach (var user in testUsers)
                {
                    user.IsDeleted = true;
                    user.DeletedAt = DateTime.UtcNow;
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation($"Cleared {testUsers.Count()} test users and related data");
                return (true, $"Successfully cleared {testUsers.Count()} test users and all related seeded data.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while clearing seeded data");
                return (false, $"Error while clearing data: {ex.Message}");
            }
        }
    }
}