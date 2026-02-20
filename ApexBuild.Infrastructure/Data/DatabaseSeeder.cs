using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using Microsoft.Extensions.Logging;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Infrastructure.Data
{
    public class DatabaseSeeder
    {
        // Helper: create a UTC DateTime from y/m/d to satisfy Npgsql 'timestamp with time zone'
        private static DateTime D(int y, int m, int d) => new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc);
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<DatabaseSeeder> _logger;

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
                var existingSuperAdmin = await _unitOfWork.Users.GetByEmailAsync("superadmin@apexbuild.io", cancellationToken);
                if (existingSuperAdmin != null)
                {
                    _logger.LogInformation("Database already seeded. Skipping.");
                    return (true, "Database already seeded.");
                }
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                var roles       = await SeedRolesAsync(cancellationToken);
                var users       = await SeedUsersAsync(cancellationToken);
                await SeedUserRolesAsync(users, roles, cancellationToken);
                var orgs        = await SeedOrganizationsAsync(users, cancellationToken);
                await SeedOrganizationMembersAsync(orgs, users, cancellationToken);
                await SeedSubscriptionsAsync(orgs, users, cancellationToken);
                var projects    = await SeedProjectsAsync(orgs, users, cancellationToken);
                var departments = await SeedDepartmentsAsync(projects, users, cancellationToken);
                var contractors = await SeedContractorsAsync(projects, departments, users, cancellationToken);
                await LinkDepartmentsToContractorsAsync(departments, contractors, cancellationToken);
                var milestones  = await SeedMilestonesAsync(projects, cancellationToken);
                await SeedProjectUsersAsync(projects, users, roles, cancellationToken);
                var tasks       = await SeedTasksAsync(projects, departments, contractors, milestones, users, cancellationToken);
                await SeedSubtasksAsync(tasks, users, cancellationToken);
                await SeedTaskUsersAsync(cancellationToken);
                await SeedTaskUpdatesAsync(tasks, users, cancellationToken);
                await SeedTaskCommentsAsync(tasks, users, cancellationToken);
                await SeedWorkInfosAsync(projects, departments, contractors, users, orgs, cancellationToken);
                await SeedInvitationsAsync(projects, users, roles, cancellationToken);
                await SeedNotificationsAsync(users, cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                _logger.LogInformation("Database seeding completed successfully.");
                return (true, "Database seeded successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Database seeding failed.");
                var inner = ex.InnerException;
                var msgs = new System.Text.StringBuilder();
                msgs.Append(ex.Message);
                while (inner != null) { msgs.Append(" | INNER: "); msgs.Append(inner.Message); inner = inner.InnerException; }
                return (false, $"Seeding failed: {msgs}");
            }
        }

        // ============================================================
        //  ROLES
        // ============================================================
        private async Task<Dictionary<string, Role>> SeedRolesAsync(CancellationToken ct)
        {
            _logger.LogInformation("Seeding roles...");
            var defs = new[]
            {
                new { Name = "SuperAdmin",           Type = RoleType.SuperAdmin,           Level = 1, Desc = "Platform super administrator with full system access",         IsSystem = true  },
                new { Name = "PlatformAdmin",        Type = RoleType.PlatformAdmin,        Level = 2, Desc = "Platform administrator who creates and manages organizations",  IsSystem = true  },
                new { Name = "ProjectOwner",         Type = RoleType.ProjectOwner,         Level = 3, Desc = "Owner of a specific construction project",                     IsSystem = false },
                new { Name = "ProjectAdministrator", Type = RoleType.ProjectAdministrator, Level = 4, Desc = "Administrator of a specific construction project",              IsSystem = false },
                new { Name = "ContractorAdmin",      Type = RoleType.ContractorAdmin,      Level = 5, Desc = "Head of a contractor company team on a project",               IsSystem = false },
                new { Name = "DepartmentSupervisor", Type = RoleType.DepartmentSupervisor, Level = 6, Desc = "Supervisor of a department within a project",                  IsSystem = false },
                new { Name = "FieldWorker",          Type = RoleType.FieldWorker,          Level = 7, Desc = "Field worker assigned to and completing tasks",                 IsSystem = false },
                new { Name = "Observer",             Type = RoleType.Observer,             Level = 8, Desc = "Read-only observer of a project",                              IsSystem = false },
            };
            var result = new Dictionary<string, Role>();
            foreach (var d in defs)
            {
                var existing = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == d.Name, ct);
                if (existing != null) { result[d.Name] = existing; continue; }
                var role = new Role
                {
                    Id = Guid.NewGuid(), Name = d.Name, RoleType = d.Type,
                    Level = d.Level, Description = d.Desc, IsSystemRole = d.IsSystem,
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Roles.AddAsync(role, ct);
                result[d.Name] = role;
            }
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} roles.", result.Count);
            return result;
        }

        // ============================================================
        //  USERS
        // ============================================================
        private async Task<Dictionary<string, User>> SeedUsersAsync(CancellationToken ct)
        {
            _logger.LogInformation("Seeding users...");
            var hash = _passwordHasher.HashPassword("Password123%");
            var defs = new[]
            {
                new { Email = "superadmin@apexbuild.io",      First = "Alex",    Last = "Dev",     Phone = "+2348000000001" },
                new { Email = "james.okafor@mailinator.com",  First = "James",   Last = "Okafor",  Phone = "+2348000000002" },
                new { Email = "linda.stone@mailinator.com",   First = "Linda",   Last = "Stone",   Phone = "+2348000000003" },
                new { Email = "michael.chen@mailinator.com",  First = "Michael", Last = "Chen",    Phone = "+2348000000004" },
                new { Email = "grace.eze@mailinator.com",     First = "Grace",   Last = "Eze",     Phone = "+2348000000005" },
                new { Email = "tony.adeyemi@mailinator.com",  First = "Tony",    Last = "Adeyemi", Phone = "+2348000000006" },
                new { Email = "rita.obi@mailinator.com",      First = "Rita",    Last = "Obi",     Phone = "+2348000000007" },
                new { Email = "samuel.king@mailinator.com",   First = "Samuel",  Last = "King",    Phone = "+2348000000008" },
                new { Email = "fatima.bello@mailinator.com",  First = "Fatima",  Last = "Bello",   Phone = "+2348000000009" },
                new { Email = "dan.foster@mailinator.com",    First = "Dan",     Last = "Foster",  Phone = "+2348000000010" },
                new { Email = "ken.park@mailinator.com",      First = "Ken",     Last = "Park",    Phone = "+2348000000011" },
                new { Email = "amaka.nwosu@mailinator.com",   First = "Amaka",   Last = "Nwosu",   Phone = "+2348000000012" },
                new { Email = "david.osei@mailinator.com",    First = "David",   Last = "Osei",    Phone = "+2348000000013" },
                new { Email = "chloe.west@mailinator.com",    First = "Chloe",   Last = "West",    Phone = "+2348000000014" },
            };
            var result = new Dictionary<string, User>();
            foreach (var d in defs)
            {
                var existing = await _unitOfWork.Users.GetByEmailAsync(d.Email, ct);
                if (existing != null) { result[d.Email] = existing; continue; }
                var user = new User
                {
                    Id = Guid.NewGuid(), Email = d.Email, FirstName = d.First, LastName = d.Last,
                    PhoneNumber = d.Phone, PasswordHash = hash, Status = UserStatus.Active,
                    EmailConfirmed = true, EmailConfirmedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Users.AddAsync(user, ct);
                result[d.Email] = user;
            }
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} users.", result.Count);
            return result;
        }

        // ============================================================
        //  USER ROLES (system-level)
        // ============================================================
        private async Task SeedUserRolesAsync(Dictionary<string, User> users, Dictionary<string, Role> roles, CancellationToken ct)
        {
            _logger.LogInformation("Seeding system-level user roles...");
            var assignments = new[]
            {
                new { Email = "superadmin@apexbuild.io",     RoleName = "SuperAdmin"    },
                new { Email = "james.okafor@mailinator.com", RoleName = "PlatformAdmin" },
                new { Email = "linda.stone@mailinator.com",  RoleName = "PlatformAdmin" },
                new { Email = "michael.chen@mailinator.com", RoleName = "PlatformAdmin" },
            };
            foreach (var a in assignments)
            {
                var user = users[a.Email]; var role = roles[a.RoleName];
                if (await _unitOfWork.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id && ur.ProjectId == null, ct)) continue;
                await _unitOfWork.UserRoles.AddAsync(new UserRole
                {
                    Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id,
                    IsActive = true, ActivatedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
                }, ct);
            }
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded system-level user roles.");
        }

        // ============================================================
        //  ORGANIZATIONS
        // ============================================================
        private async Task<Dictionary<string, Organization>> SeedOrganizationsAsync(Dictionary<string, User> users, CancellationToken ct)
        {
            _logger.LogInformation("Seeding organizations...");
            var result = new Dictionary<string, Organization>();
            var okafor = await _unitOfWork.Organizations.FirstOrDefaultAsync(o => o.Code == "ORG-2025-001", ct);
            if (okafor == null)
            {
                okafor = new Organization {
                    Id = Guid.NewGuid(), Name = "OkaforBuilds Ltd", Code = "ORG-2025-001",
                    Description = "A leading construction company based in Lagos, Nigeria.",
                    Email = "info@okaforbuilds.com", PhoneNumber = "+2348100000001",
                    Country = "Nigeria", City = "Lagos", State = "Lagos",
                    OwnerId = users["james.okafor@mailinator.com"].Id,
                    IsActive = true, IsVerified = true, VerifiedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Organizations.AddAsync(okafor, ct);
            }
            result["OkaforBuilds"] = okafor;
            var stone = await _unitOfWork.Organizations.FirstOrDefaultAsync(o => o.Code == "ORG-2025-002", ct);
            if (stone == null)
            {
                stone = new Organization {
                    Id = Guid.NewGuid(), Name = "Stone & Partners Construction", Code = "ORG-2025-002",
                    Description = "Specialist construction and renovation firm.",
                    Email = "info@stoneandpartners.com", PhoneNumber = "+2348100000002",
                    Country = "Nigeria", City = "Lagos", State = "Lagos",
                    OwnerId = users["linda.stone@mailinator.com"].Id,
                    IsActive = true, IsVerified = true, VerifiedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Organizations.AddAsync(stone, ct);
            }
            result["StonePartners"] = stone;
            var superDirect = await _unitOfWork.Organizations.FirstOrDefaultAsync(o => o.Code == "ORG-2025-000", ct);
            if (superDirect == null)
            {
                superDirect = new Organization {
                    Id = Guid.NewGuid(), Name = "SuperAdmin Direct", Code = "ORG-2025-000",
                    Description = "SuperAdmin-owned organization for platform-managed projects.",
                    Email = "superadmin@apexbuild.io", PhoneNumber = "+2348000000001",
                    Country = "Nigeria", City = "Abuja", State = "FCT",
                    OwnerId = users["superadmin@apexbuild.io"].Id,
                    IsActive = true, IsVerified = true, VerifiedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Organizations.AddAsync(superDirect, ct);
            }
            result["SuperAdminDirect"] = superDirect;
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} organizations.", result.Count);
            return result;
        }

        // ============================================================
        //  ORGANIZATION MEMBERS
        // ============================================================
        private async Task SeedOrganizationMembersAsync(Dictionary<string, Organization> orgs, Dictionary<string, User> users, CancellationToken ct)
        {
            _logger.LogInformation("Seeding organization members...");
            var memberships = new[]
            {
                new { OrgKey = "OkaforBuilds",     Email = "james.okafor@mailinator.com",  Position = "Managing Director"     },
                new { OrgKey = "OkaforBuilds",     Email = "tony.adeyemi@mailinator.com",   Position = "Project Administrator" },
                new { OrgKey = "OkaforBuilds",     Email = "grace.eze@mailinator.com",      Position = "Project Owner"         },
                new { OrgKey = "OkaforBuilds",     Email = "rita.obi@mailinator.com",        Position = "Department Supervisor" },
                new { OrgKey = "OkaforBuilds",     Email = "samuel.king@mailinator.com",    Position = "Department Supervisor" },
                new { OrgKey = "OkaforBuilds",     Email = "dan.foster@mailinator.com",     Position = "Field Worker"          },
                new { OrgKey = "OkaforBuilds",     Email = "ken.park@mailinator.com",       Position = "Contractor Admin"      },
                new { OrgKey = "OkaforBuilds",     Email = "fatima.bello@mailinator.com",   Position = "Field Worker"          },
                new { OrgKey = "OkaforBuilds",     Email = "david.osei@mailinator.com",     Position = "Contractor Admin"      },
                new { OrgKey = "OkaforBuilds",     Email = "amaka.nwosu@mailinator.com",    Position = "Department Supervisor" },
                new { OrgKey = "OkaforBuilds",     Email = "michael.chen@mailinator.com",   Position = "Observer"              },
                new { OrgKey = "StonePartners",    Email = "linda.stone@mailinator.com",    Position = "Managing Director"     },
                new { OrgKey = "StonePartners",    Email = "michael.chen@mailinator.com",   Position = "Project Administrator" },
                new { OrgKey = "StonePartners",    Email = "chloe.west@mailinator.com",     Position = "Department Supervisor" },
                new { OrgKey = "StonePartners",    Email = "samuel.king@mailinator.com",    Position = "Field Worker"          },
                new { OrgKey = "SuperAdminDirect", Email = "superadmin@apexbuild.io",       Position = "Super Administrator"   },
                new { OrgKey = "SuperAdminDirect", Email = "linda.stone@mailinator.com",    Position = "Project Administrator" },
            };
            foreach (var m in memberships)
            {
                var org = orgs[m.OrgKey]; var user = users[m.Email];
                if (await _unitOfWork.OrganizationMembers.AnyAsync(om => om.OrganizationId == org.Id && om.UserId == user.Id, ct)) continue;
                await _unitOfWork.OrganizationMembers.AddAsync(new OrganizationMember
                {
                    Id = Guid.NewGuid(), OrganizationId = org.Id, UserId = user.Id,
                    Position = m.Position, IsActive = true, JoinedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
                }, ct);
            }
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded organization members.");
        }

        // ============================================================
        //  SUBSCRIPTIONS
        // ============================================================
        private async Task SeedSubscriptionsAsync(Dictionary<string, Organization> orgs, Dictionary<string, User> users, CancellationToken ct)
        {
            _logger.LogInformation("Seeding subscriptions...");
            if (!await _unitOfWork.Subscriptions.AnyAsync(s => s.OrganizationId == orgs["OkaforBuilds"].Id, ct))
                await _unitOfWork.Subscriptions.AddAsync(new Subscription {
                    Id = Guid.NewGuid(), OrganizationId = orgs["OkaforBuilds"].Id,
                    UserId = users["james.okafor@mailinator.com"].Id,
                    UserMonthlyRate = 20m, ActiveUserCount = 6, IsFreePlan = false,
                    Status = SubscriptionStatus.Active, BillingCycle = SubscriptionBillingCycle.Monthly,
                    BillingStartDate = DateTime.UtcNow, BillingEndDate = DateTime.UtcNow.AddMonths(1),
                    NextBillingDate = DateTime.UtcNow.AddMonths(1), Amount = 6 * 20m,
                    AutoRenew = true, CreatedAt = DateTime.UtcNow,
                }, ct);
            if (!await _unitOfWork.Subscriptions.AnyAsync(s => s.OrganizationId == orgs["StonePartners"].Id, ct))
                await _unitOfWork.Subscriptions.AddAsync(new Subscription {
                    Id = Guid.NewGuid(), OrganizationId = orgs["StonePartners"].Id,
                    UserId = users["linda.stone@mailinator.com"].Id,
                    UserMonthlyRate = 20m, ActiveUserCount = 4, IsFreePlan = false,
                    Status = SubscriptionStatus.Active, BillingCycle = SubscriptionBillingCycle.Monthly,
                    BillingStartDate = DateTime.UtcNow, BillingEndDate = DateTime.UtcNow.AddMonths(1),
                    NextBillingDate = DateTime.UtcNow.AddMonths(1), Amount = 4 * 20m,
                    AutoRenew = true, CreatedAt = DateTime.UtcNow,
                }, ct);
            if (!await _unitOfWork.Subscriptions.AnyAsync(s => s.OrganizationId == orgs["SuperAdminDirect"].Id, ct))
                await _unitOfWork.Subscriptions.AddAsync(new Subscription {
                    Id = Guid.NewGuid(), OrganizationId = orgs["SuperAdminDirect"].Id,
                    UserId = users["superadmin@apexbuild.io"].Id,
                    UserMonthlyRate = 0m, ActiveUserCount = 0, IsFreePlan = true,
                    Status = SubscriptionStatus.Active, BillingCycle = SubscriptionBillingCycle.Monthly,
                    BillingStartDate = DateTime.UtcNow, BillingEndDate = DateTime.UtcNow.AddMonths(1),
                    NextBillingDate = DateTime.UtcNow.AddMonths(1), Amount = 0m,
                    AutoRenew = true, CreatedAt = DateTime.UtcNow,
                }, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded subscriptions.");
        }

        // ============================================================
        //  PROJECTS
        // ============================================================
        private async Task<Dictionary<string, Project>> SeedProjectsAsync(Dictionary<string, Organization> orgs, Dictionary<string, User> users, CancellationToken ct)
        {
            _logger.LogInformation("Seeding projects...");
            var result = new Dictionary<string, Project>();

            // Project 1: Eko Atlantic Tower
            var proj1 = await _unitOfWork.Projects.FirstOrDefaultAsync(p => p.Code == "PROJ-2025-001", ct);
            if (proj1 == null)
            {
                proj1 = new Project {
                    Id = Guid.NewGuid(), Name = "Eko Atlantic Tower", Code = "PROJ-2025-001",
                    OrganizationId = orgs["OkaforBuilds"].Id,
                    Description = "A landmark 40-story mixed-use tower on Eko Atlantic City, Lagos Island.",
                    Status = ProjectStatus.Active, ProjectType = ProjectType.Building,
                    Location = "Lagos Island, Lagos", IsActive = true,
                    StartDate = D(2025, 1, 15), ExpectedEndDate = D(2027, 6, 30),
                    Budget = 850_000_000m, Currency = "NGN",
                    ProjectOwnerId = users["james.okafor@mailinator.com"].Id,
                    ProjectAdminId = users["tony.adeyemi@mailinator.com"].Id,
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Projects.AddAsync(proj1, ct);
            }
            result["EkoAtlantic"] = proj1;

            // Project 2: Abuja Ring Road Extension
            var proj2 = await _unitOfWork.Projects.FirstOrDefaultAsync(p => p.Code == "PROJ-2025-002", ct);
            if (proj2 == null)
            {
                proj2 = new Project {
                    Id = Guid.NewGuid(), Name = "Abuja Ring Road Extension", Code = "PROJ-2025-002",
                    OrganizationId = orgs["OkaforBuilds"].Id,
                    Description = "Extension of the Abuja ring road to ease traffic congestion in the FCT.",
                    Status = ProjectStatus.Active, ProjectType = ProjectType.Road,
                    Location = "FCT, Abuja", IsActive = true,
                    StartDate = D(2025, 3, 1), ExpectedEndDate = D(2028, 12, 31),
                    Budget = 2_400_000_000m, Currency = "NGN",
                    ProjectOwnerId = users["grace.eze@mailinator.com"].Id,
                    ProjectAdminId = users["james.okafor@mailinator.com"].Id,
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Projects.AddAsync(proj2, ct);
            }
            result["AbujaRingRoad"] = proj2;

            // Project 3: Lagos State Hospital Renovation
            var proj3 = await _unitOfWork.Projects.FirstOrDefaultAsync(p => p.Code == "PROJ-2025-003", ct);
            if (proj3 == null)
            {
                proj3 = new Project {
                    Id = Guid.NewGuid(), Name = "Lagos State Hospital Renovation", Code = "PROJ-2025-003",
                    OrganizationId = orgs["StonePartners"].Id,
                    Description = "Comprehensive renovation and upgrade of Lagos State General Hospital, Ikeja.",
                    Status = ProjectStatus.Active, ProjectType = ProjectType.Hospital,
                    Location = "Ikeja, Lagos", IsActive = true,
                    StartDate = D(2025, 2, 1), ExpectedEndDate = D(2026, 8, 31),
                    Budget = 320_000_000m, Currency = "NGN",
                    ProjectOwnerId = users["linda.stone@mailinator.com"].Id,
                    ProjectAdminId = users["michael.chen@mailinator.com"].Id,
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Projects.AddAsync(proj3, ct);
            }
            result["HospitalReno"] = proj3;

            // Project 4: Presidential Garden Park
            var proj4 = await _unitOfWork.Projects.FirstOrDefaultAsync(p => p.Code == "PROJ-2025-004", ct);
            if (proj4 == null)
            {
                proj4 = new Project {
                    Id = Guid.NewGuid(), Name = "Presidential Garden Park", Code = "PROJ-2025-004",
                    OrganizationId = orgs["SuperAdminDirect"].Id,
                    Description = "Development of the Presidential Garden and recreational park in Abuja.",
                    Status = ProjectStatus.Planning, ProjectType = ProjectType.Park,
                    Location = "Abuja", IsActive = true,
                    Budget = 75_000_000m, Currency = "NGN",
                    ProjectOwnerId = users["superadmin@apexbuild.io"].Id,
                    ProjectAdminId = users["linda.stone@mailinator.com"].Id,
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Projects.AddAsync(proj4, ct);
            }
            result["PresidentialGarden"] = proj4;

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} projects.", result.Count);
            return result;
        }

        // ============================================================
        //  DEPARTMENTS
        // ============================================================
        private async Task<Dictionary<string, Department>> SeedDepartmentsAsync(Dictionary<string, Project> projects, Dictionary<string, User> users, CancellationToken ct)
        {
            _logger.LogInformation("Seeding departments...");
            var result = new Dictionary<string, Department>();

            async Task<Department> EnsureDept(string key, string code, string name, Guid projId, Guid supId, bool isOut, string spec, DateTime start)
            {
                var dept = await _unitOfWork.Departments.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (dept != null) { result[key] = dept; return dept; }
                dept = new Department {
                    Id = Guid.NewGuid(), Name = name, Code = code, ProjectId = projId,
                    SupervisorId = supId, IsOutsourced = isOut, Specialization = spec,
                    Status = DepartmentStatus.Active, IsActive = true, StartDate = start, CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Departments.AddAsync(dept, ct);
                result[key] = dept;
                return dept;
            }

            // Eko Atlantic Tower departments
            var ekoId = projects["EkoAtlantic"].Id;
            await EnsureDept("StructuralEng", "DEPT-STR-001", "Structural Engineering",  ekoId, users["tony.adeyemi@mailinator.com"].Id,  false, "Structural Engineering", D(2025, 1, 15));
            await EnsureDept("ElectricalSys", "DEPT-ELE-001", "Electrical Systems",      ekoId, users["rita.obi@mailinator.com"].Id,       true,  "Electrical",             D(2025, 1, 15));
            await EnsureDept("PlumbingUtil",  "DEPT-PLU-001", "Plumbing & Utilities",    ekoId, users["samuel.king@mailinator.com"].Id,    true,  "Plumbing",               D(2025, 1, 15));
            await EnsureDept("InteriorFin",   "DEPT-INT-001", "Interior Finishing",      ekoId, users["amaka.nwosu@mailinator.com"].Id,    false, "Interior Finishing",     D(2025, 1, 15));

            // Abuja Ring Road departments
            var roadId = projects["AbujaRingRoad"].Id;
            await EnsureDept("FoundationEarth", "DEPT-FND-001", "Foundation & Earthworks", roadId, users["dan.foster@mailinator.com"].Id, false, "Earthworks",        D(2025, 3, 1));
            await EnsureDept("RoadConstruct",   "DEPT-RDC-001", "Road Construction",       roadId, users["ken.park@mailinator.com"].Id,   true,  "Road Construction", D(2025, 3, 1));

            // Hospital Renovation departments
            var hospId = projects["HospitalReno"].Id;
            await EnsureDept("MedicalSystems", "DEPT-MED-001", "Medical Systems", hospId, users["chloe.west@mailinator.com"].Id,   false, "Medical Systems",   D(2025, 2, 1));
            await EnsureDept("CivilWorks",     "DEPT-CIV-001", "Civil Works",     hospId, users["michael.chen@mailinator.com"].Id, false, "Civil Engineering", D(2025, 2, 1));

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} departments.", result.Count);
            return result;
        }

        // ============================================================
        //  CONTRACTORS
        // ============================================================
        private async Task<Dictionary<string, Contractor>> SeedContractorsAsync(Dictionary<string, Project> projects, Dictionary<string, Department> departments, Dictionary<string, User> users, CancellationToken ct)
        {
            _logger.LogInformation("Seeding contractors...");
            var result = new Dictionary<string, Contractor>();

            // FastWire Electricals
            var fw = await _unitOfWork.Contractors.FirstOrDefaultAsync(c => c.Code == "CONTR-2025-001", ct);
            if (fw == null)
            {
                fw = new Contractor {
                    Id = Guid.NewGuid(), CompanyName = "FastWire Electricals Ltd", Code = "CONTR-2025-001",
                    ProjectId = projects["EkoAtlantic"].Id, DepartmentId = departments["ElectricalSys"].Id,
                    ContractorAdminId = users["ken.park@mailinator.com"].Id,
                    Specialization = "Electrical",
                    Description = "Specialist electrical contractor for the Eko Atlantic Tower project.",
                    ContractStartDate = D(2025, 3, 1), ContractEndDate = D(2026, 9, 30),
                    ContractValue = 45_000_000m, Currency = "NGN", Status = ContractorStatus.Active, CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Contractors.AddAsync(fw, ct);
            }
            result["FastWire"] = fw;

            // AquaTech Ltd
            var at = await _unitOfWork.Contractors.FirstOrDefaultAsync(c => c.Code == "CONTR-2025-002", ct);
            if (at == null)
            {
                at = new Contractor {
                    Id = Guid.NewGuid(), CompanyName = "AquaTech Plumbing Solutions", Code = "CONTR-2025-002",
                    ProjectId = projects["EkoAtlantic"].Id, DepartmentId = departments["PlumbingUtil"].Id,
                    ContractorAdminId = users["david.osei@mailinator.com"].Id,
                    Specialization = "Plumbing",
                    Description = "Specialist plumbing and utilities contractor for Eko Atlantic Tower.",
                    ContractStartDate = D(2025, 4, 1), ContractEndDate = D(2026, 6, 30),
                    ContractValue = 28_000_000m, Currency = "NGN", Status = ContractorStatus.Active, CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Contractors.AddAsync(at, ct);
            }
            result["AquaTech"] = at;

            // RoadMaster Co.
            var rm = await _unitOfWork.Contractors.FirstOrDefaultAsync(c => c.Code == "CONTR-2025-003", ct);
            if (rm == null)
            {
                rm = new Contractor {
                    Id = Guid.NewGuid(), CompanyName = "RoadMaster Construction Co.", Code = "CONTR-2025-003",
                    ProjectId = projects["AbujaRingRoad"].Id, DepartmentId = departments["RoadConstruct"].Id,
                    ContractorAdminId = users["fatima.bello@mailinator.com"].Id,
                    Specialization = "Road Construction",
                    Description = "Major road construction contractor for the Abuja Ring Road Extension.",
                    ContractStartDate = D(2025, 6, 1), ContractEndDate = D(2028, 11, 30),
                    ContractValue = 980_000_000m, Currency = "NGN", Status = ContractorStatus.Active, CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Contractors.AddAsync(rm, ct);
            }
            result["RoadMaster"] = rm;

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} contractors.", result.Count);
            return result;
        }

        // ============================================================
        //  LINK DEPARTMENTS TO CONTRACTORS
        // ============================================================
        private async Task LinkDepartmentsToContractorsAsync(Dictionary<string, Department> departments, Dictionary<string, Contractor> contractors, CancellationToken ct)
        {
            _logger.LogInformation("Linking departments to contractors...");
            var elec = departments["ElectricalSys"];
            if (elec.ContractorId == null) { elec.ContractorId = contractors["FastWire"].Id; await _unitOfWork.Departments.UpdateAsync(elec, ct); }
            var plumb = departments["PlumbingUtil"];
            if (plumb.ContractorId == null) { plumb.ContractorId = contractors["AquaTech"].Id; await _unitOfWork.Departments.UpdateAsync(plumb, ct); }
            var road = departments["RoadConstruct"];
            if (road.ContractorId == null) { road.ContractorId = contractors["RoadMaster"].Id; await _unitOfWork.Departments.UpdateAsync(road, ct); }
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Linked departments to contractors.");
        }

        // ============================================================
        //  PROJECT MILESTONES
        // ============================================================
        private async Task<Dictionary<string, ProjectMilestone>> SeedMilestonesAsync(Dictionary<string, Project> projects, CancellationToken ct)
        {
            _logger.LogInformation("Seeding project milestones...");
            var result = new Dictionary<string, ProjectMilestone>();
            var ekoId  = projects["EkoAtlantic"].Id;
            var roadId = projects["AbujaRingRoad"].Id;

            async Task<ProjectMilestone> E(string key, Guid projId, string title, string desc, DateTime due, MilestoneStatus st, decimal prog, int order, DateTime? completedAt = null)
            {
                var m = await _unitOfWork.Milestones.FirstOrDefaultAsync(x => x.ProjectId == projId && x.Title == title, ct);
                if (m != null) { result[key] = m; return m; }
                m = new ProjectMilestone {
                    Id = Guid.NewGuid(), Title = title, Description = desc, ProjectId = projId,
                    DueDate = due, CompletedAt = completedAt, Status = st, Progress = prog,
                    OrderIndex = order, CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Milestones.AddAsync(m, ct);
                result[key] = m;
                return m;
            }

            await E("EkoFoundation",      ekoId,  "Foundation Complete",       "All foundation work including piling and ground beams completed.",                   D(2025, 9, 30), MilestoneStatus.Completed,  100, 1, D(2025, 9, 28));
            await E("EkoStructuralFrame", ekoId,  "Structural Frame Complete", "Steel and concrete structural frame completed up to the roof level.",               D(2026, 3, 31), MilestoneStatus.InProgress, 35,  2);
            await E("EkoMEP",             ekoId,  "MEP Rough-In Complete",     "All mechanical, electrical and plumbing rough-in work completed.",                  D(2026, 9, 30), MilestoneStatus.Upcoming,   0,   3);
            await E("RoadEarthworks",     roadId, "Phase 1 Earthworks Done",   "Completion of all earthworks and site preparation for Phase 1 of the ring road.",  D(2026, 6, 30), MilestoneStatus.Upcoming,   0,   1);

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} milestones.", result.Count);
            return result;
        }

        // ============================================================
        //  PROJECT USERS
        // ============================================================
        private async Task SeedProjectUsersAsync(Dictionary<string, Project> projects, Dictionary<string, User> users, Dictionary<string, Role> roles, CancellationToken ct)
        {
            _logger.LogInformation("Seeding project users...");
            var assignments = new[]
            {
                // Eko Atlantic Tower
                new { ProjKey = "EkoAtlantic",        Email = "james.okafor@mailinator.com",  RoleKey = "ProjectOwner"         },
                new { ProjKey = "EkoAtlantic",        Email = "tony.adeyemi@mailinator.com",   RoleKey = "ProjectAdministrator" },
                new { ProjKey = "EkoAtlantic",        Email = "rita.obi@mailinator.com",        RoleKey = "DepartmentSupervisor" },
                new { ProjKey = "EkoAtlantic",        Email = "samuel.king@mailinator.com",    RoleKey = "DepartmentSupervisor" },
                new { ProjKey = "EkoAtlantic",        Email = "amaka.nwosu@mailinator.com",    RoleKey = "DepartmentSupervisor" },
                new { ProjKey = "EkoAtlantic",        Email = "ken.park@mailinator.com",       RoleKey = "ContractorAdmin"      },
                new { ProjKey = "EkoAtlantic",        Email = "david.osei@mailinator.com",     RoleKey = "ContractorAdmin"      },
                new { ProjKey = "EkoAtlantic",        Email = "fatima.bello@mailinator.com",   RoleKey = "FieldWorker"          },
                new { ProjKey = "EkoAtlantic",        Email = "chloe.west@mailinator.com",     RoleKey = "FieldWorker"          },
                new { ProjKey = "EkoAtlantic",        Email = "dan.foster@mailinator.com",     RoleKey = "FieldWorker"          },
                // Abuja Ring Road
                new { ProjKey = "AbujaRingRoad",      Email = "grace.eze@mailinator.com",      RoleKey = "ProjectOwner"         },
                new { ProjKey = "AbujaRingRoad",      Email = "james.okafor@mailinator.com",   RoleKey = "ProjectAdministrator" },
                new { ProjKey = "AbujaRingRoad",      Email = "dan.foster@mailinator.com",     RoleKey = "DepartmentSupervisor" },
                new { ProjKey = "AbujaRingRoad",      Email = "ken.park@mailinator.com",       RoleKey = "DepartmentSupervisor" },
                new { ProjKey = "AbujaRingRoad",      Email = "fatima.bello@mailinator.com",   RoleKey = "ContractorAdmin"      },
                new { ProjKey = "AbujaRingRoad",      Email = "rita.obi@mailinator.com",        RoleKey = "FieldWorker"          },
                new { ProjKey = "AbujaRingRoad",      Email = "michael.chen@mailinator.com",   RoleKey = "Observer"             },
                // Hospital Renovation
                new { ProjKey = "HospitalReno",       Email = "linda.stone@mailinator.com",    RoleKey = "ProjectOwner"         },
                new { ProjKey = "HospitalReno",       Email = "michael.chen@mailinator.com",   RoleKey = "ProjectAdministrator" },
                new { ProjKey = "HospitalReno",       Email = "chloe.west@mailinator.com",     RoleKey = "DepartmentSupervisor" },
                new { ProjKey = "HospitalReno",       Email = "samuel.king@mailinator.com",    RoleKey = "FieldWorker"          },
                // Presidential Garden
                new { ProjKey = "PresidentialGarden", Email = "superadmin@apexbuild.io",       RoleKey = "ProjectOwner"         },
                new { ProjKey = "PresidentialGarden", Email = "linda.stone@mailinator.com",    RoleKey = "ProjectAdministrator" },
            };
            foreach (var a in assignments)
            {
                var project = projects[a.ProjKey]; var user = users[a.Email]; var role = roles[a.RoleKey];
                if (!await _unitOfWork.ProjectUsers.AnyAsync(pu => pu.ProjectId == project.Id && pu.UserId == user.Id, ct))
                    await _unitOfWork.ProjectUsers.AddAsync(new ProjectUser {
                        Id = Guid.NewGuid(), ProjectId = project.Id, UserId = user.Id, RoleId = role.Id,
                        Status = ProjectUserStatus.Active, JoinedAt = DateTime.UtcNow, IsActive = true, CreatedAt = DateTime.UtcNow,
                    }, ct);
                if (!await _unitOfWork.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id && ur.ProjectId == project.Id, ct))
                    await _unitOfWork.UserRoles.AddAsync(new UserRole {
                        Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id, ProjectId = project.Id,
                        IsActive = true, ActivatedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
                    }, ct);
            }
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded project users and project-scoped user roles.");
        }

        // TASKS
        // ============================================================
        //  TASKS
        // ============================================================
        private async Task<Dictionary<string, ProjectTask>> SeedTasksAsync(
            Dictionary<string, Project> projects,
            Dictionary<string, Department> departments,
            Dictionary<string, Contractor> contractors,
            Dictionary<string, ProjectMilestone> milestones,
            Dictionary<string, User> users,
            CancellationToken ct)
        {
            _logger.LogInformation("Seeding tasks...");
            var result = new Dictionary<string, ProjectTask>();

            async Task<ProjectTask> EnsureTask(string key, string code, ProjectTask factory)
            {
                var t = await _unitOfWork.Tasks.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (t != null) { result[key] = t; return t; }
                await _unitOfWork.Tasks.AddAsync(factory, ct);
                result[key] = factory; return factory;
            }

            await EnsureTask("Task1", "TASK-2025-001", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Install Load-Bearing Column Grid - Level 1-5", Code = "TASK-2025-001",
                Description = "Installation of the primary load-bearing column grid from ground level to Level 5.",
                ProjectId = projects["EkoAtlantic"].Id, DepartmentId = departments["StructuralEng"].Id,
                AssignedToUserId = users["dan.foster@mailinator.com"].Id, AssignedByUserId = users["tony.adeyemi@mailinator.com"].Id,
                MilestoneId = milestones["EkoStructuralFrame"].Id,
                Status = TaskStatus.InProgress, Priority = 4, Progress = 65, EstimatedHours = 480,
                StartDate = D(2025, 6, 1), DueDate = D(2025, 11, 30), CreatedAt = DateTime.UtcNow,
            });
            await EnsureTask("Task2", "TASK-2025-002", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Main Electrical Panel & Distribution Board", Code = "TASK-2025-002",
                Description = "Supply and installation of the main electrical panel and distribution boards for all floors.",
                ProjectId = projects["EkoAtlantic"].Id, DepartmentId = departments["ElectricalSys"].Id,
                ContractorId = contractors["FastWire"].Id,
                AssignedToUserId = users["chloe.west@mailinator.com"].Id, AssignedByUserId = users["ken.park@mailinator.com"].Id,
                MilestoneId = milestones["EkoMEP"].Id,
                Status = TaskStatus.UnderReview, Priority = 3, Progress = 80, EstimatedHours = 240, CreatedAt = DateTime.UtcNow,
            });
            await EnsureTask("Task3", "TASK-2025-003", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Install Drainage & Sewer System - Floors 1-10", Code = "TASK-2025-003",
                Description = "Complete drainage and sewer system installation for floors 1-10 of the tower.",
                ProjectId = projects["EkoAtlantic"].Id, DepartmentId = departments["PlumbingUtil"].Id,
                ContractorId = contractors["AquaTech"].Id,
                AssignedToUserId = users["samuel.king@mailinator.com"].Id, AssignedByUserId = users["david.osei@mailinator.com"].Id,
                Status = TaskStatus.InProgress, Priority = 3, Progress = 40, CreatedAt = DateTime.UtcNow,
            });
            await EnsureTask("Task4", "TASK-2025-004", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Lobby & Reception Interior Finishing", Code = "TASK-2025-004",
                Description = "Complete interior finishing works for the ground floor lobby and reception areas.",
                ProjectId = projects["EkoAtlantic"].Id, DepartmentId = departments["InteriorFin"].Id,
                AssignedToUserId = users["amaka.nwosu@mailinator.com"].Id, AssignedByUserId = users["tony.adeyemi@mailinator.com"].Id,
                Status = TaskStatus.NotStarted, Priority = 2, Progress = 0, EstimatedHours = 320, CreatedAt = DateTime.UtcNow,
            });
            await EnsureTask("Task5", "TASK-2025-005", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Site Clearing & Topographic Survey", Code = "TASK-2025-005",
                Description = "Complete site clearing and full topographic survey of the ring road corridor.",
                ProjectId = projects["AbujaRingRoad"].Id, DepartmentId = departments["FoundationEarth"].Id,
                AssignedToUserId = users["dan.foster@mailinator.com"].Id, AssignedByUserId = users["james.okafor@mailinator.com"].Id,
                Status = TaskStatus.Completed, Priority = 4, Progress = 100, CompletedAt = D(2025, 7, 15), CreatedAt = DateTime.UtcNow,
            });
            await EnsureTask("Task6", "TASK-2025-006", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Asphalt Laying - Phase 1 (KM 0-15)", Code = "TASK-2025-006",
                Description = "Asphalt laying operations for Phase 1 covering KM 0 to KM 15 of the ring road.",
                ProjectId = projects["AbujaRingRoad"].Id, DepartmentId = departments["RoadConstruct"].Id,
                ContractorId = contractors["RoadMaster"].Id,
                AssignedToUserId = users["rita.obi@mailinator.com"].Id, AssignedByUserId = users["fatima.bello@mailinator.com"].Id,
                Status = TaskStatus.InProgress, Priority = 4, Progress = 30, EstimatedHours = 2400, CreatedAt = DateTime.UtcNow,
            });
            await EnsureTask("Task7", "TASK-2025-007", new ProjectTask {
                Id = Guid.NewGuid(), Title = "MRI Room Shielding & Medical Gas Installation", Code = "TASK-2025-007",
                Description = "Installation of lead shielding for MRI room and complete medical gas piping system.",
                ProjectId = projects["HospitalReno"].Id, DepartmentId = departments["MedicalSystems"].Id,
                AssignedToUserId = users["samuel.king@mailinator.com"].Id, AssignedByUserId = users["michael.chen@mailinator.com"].Id,
                Status = TaskStatus.InProgress, Priority = 4, Progress = 55, CreatedAt = DateTime.UtcNow,
            });
            await EnsureTask("Task8", "TASK-2025-008", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Ward Block Civil Renovation - Wings A & B", Code = "TASK-2025-008",
                Description = "Civil renovation works for Ward Block Wings A and B including structural repairs.",
                ProjectId = projects["HospitalReno"].Id, DepartmentId = departments["CivilWorks"].Id,
                AssignedToUserId = users["chloe.west@mailinator.com"].Id, AssignedByUserId = users["michael.chen@mailinator.com"].Id,
                Status = TaskStatus.NotStarted, Priority = 3, Progress = 0, CreatedAt = DateTime.UtcNow,
            });

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} tasks.", result.Count);
            return result;
        }

        // ============================================================
        //  SUBTASKS (for Task 1)
        // ============================================================
        private async Task SeedSubtasksAsync(Dictionary<string, ProjectTask> tasks, Dictionary<string, User> users, CancellationToken ct)
        {
            _logger.LogInformation("Seeding subtasks for Task 1...");
            var parent = tasks["Task1"];
            var subtaskDefs = new[]
            {
                new { Code = "TASK-2025-001-A", Title = "Survey & Mark Column Positions",  AssigneeEmail = "dan.foster@mailinator.com",  Status = TaskStatus.Completed,  Progress = 100m },
                new { Code = "TASK-2025-001-B", Title = "Excavate & Pour Footings",        AssigneeEmail = "dan.foster@mailinator.com",  Status = TaskStatus.Completed,  Progress = 100m },
                new { Code = "TASK-2025-001-C", Title = "Erect Steel Reinforcement Cages", AssigneeEmail = "chloe.west@mailinator.com",  Status = TaskStatus.InProgress, Progress = 70m  },
                new { Code = "TASK-2025-001-D", Title = "Pour Concrete & Cure",             AssigneeEmail = "dan.foster@mailinator.com",  Status = TaskStatus.NotStarted, Progress = 0m   },
            };
            foreach (var s in subtaskDefs)
            {
                if (await _unitOfWork.Tasks.AnyAsync(t => t.Code == s.Code, ct)) continue;
                await _unitOfWork.Tasks.AddAsync(new ProjectTask {
                    Id = Guid.NewGuid(), Title = s.Title, Code = s.Code,
                    Description = $"Subtask of: {parent.Title}",
                    ProjectId = parent.ProjectId, DepartmentId = parent.DepartmentId,
                    ParentTaskId = parent.Id,
                    AssignedToUserId = users[s.AssigneeEmail].Id,
                    AssignedByUserId = users["tony.adeyemi@mailinator.com"].Id,
                    Status = s.Status, Priority = 4, Progress = s.Progress,
                    CompletedAt = s.Status == TaskStatus.Completed ? DateTime.UtcNow.AddDays(-10) : (DateTime?)null,
                    CreatedAt = DateTime.UtcNow,
                }, ct);
            }
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded subtasks.");
        }

        // ============================================================
        //  TASK USERS (assignees for all seeded tasks + subtasks)
        // ============================================================
        private async Task SeedTaskUsersAsync(CancellationToken ct)
        {
            _logger.LogInformation("Seeding task user assignments...");

            // Load every non-deleted task that has an AssignedToUserId
            var allTasks = await _unitOfWork.Tasks.FindAsync(
                t => !t.IsDeleted && t.AssignedToUserId.HasValue, ct);

            foreach (var task in allTasks)
            {
                var userId = task.AssignedToUserId!.Value;
                // Idempotent: skip if the TaskUser record already exists
                var exists = await _unitOfWork.TaskUsers.AnyAsync(
                    tu => tu.TaskId == task.Id && tu.UserId == userId, ct);
                if (exists) continue;

                await _unitOfWork.TaskUsers.AddAsync(new TaskUser
                {
                    Id           = Guid.NewGuid(),
                    TaskId       = task.Id,
                    UserId       = userId,
                    AssignedByUserId = task.AssignedByUserId,
                    Role         = "Assignee",
                    IsActive     = true,
                    AssignedAt   = task.CreatedAt,
                }, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded task user assignments.");
        }

        // ============================================================
        //  TASK UPDATES
        // ============================================================
        private async Task SeedTaskUpdatesAsync(Dictionary<string, ProjectTask> tasks, Dictionary<string, User> users, CancellationToken ct)
        {
            _logger.LogInformation("Seeding task updates...");
            var chloeId  = users["chloe.west@mailinator.com"].Id;
            var ritaId   = users["rita.obi@mailinator.com"].Id;
            var samuelId = users["samuel.king@mailinator.com"].Id;
            var kenId    = users["ken.park@mailinator.com"].Id;

            // Update 1: Task 2 - Electrical Panel (ContractorAdmin approved, now UnderSupervisorReview)
            if (!await _unitOfWork.TaskUpdates.AnyAsync(u => u.TaskId == tasks["Task2"].Id && u.SubmittedByUserId == chloeId, ct))
            {
                await _unitOfWork.TaskUpdates.AddAsync(new TaskUpdate {
                    Id = Guid.NewGuid(), TaskId = tasks["Task2"].Id, SubmittedByUserId = chloeId,
                    Description = "Main distribution board fully installed. All circuit breakers tested and labeled. Load balancing verified.",
                    Status = UpdateStatus.UnderSupervisorReview,
                    MediaUrls = new List<string> {
                        "https://res.cloudinary.com/demo/image/upload/electrical_panel_1.jpg",
                        "https://res.cloudinary.com/demo/image/upload/electrical_panel_2.jpg",
                    },
                    MediaTypes = new List<string> { "image", "image" },
                    ProgressPercentage = 80, SubmittedAt = DateTime.UtcNow.AddDays(-2),
                    ContractorAdminApproved = true, ReviewedByContractorAdminId = kenId,
                    ContractorAdminReviewedAt = DateTime.UtcNow.AddDays(-1),
                    ContractorAdminFeedback = "Work quality is excellent. Panel meets all spec requirements.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                }, ct);
            }

            // Update 2: Task 6 - Asphalt Laying (UnderContractorAdminReview)
            if (!await _unitOfWork.TaskUpdates.AnyAsync(u => u.TaskId == tasks["Task6"].Id && u.SubmittedByUserId == ritaId, ct))
            {
                await _unitOfWork.TaskUpdates.AddAsync(new TaskUpdate {
                    Id = Guid.NewGuid(), TaskId = tasks["Task6"].Id, SubmittedByUserId = ritaId,
                    Description = "Asphalt laid from KM 0-4.5. Compaction tests passed. Weather delay caused 2-day stoppage.",
                    Status = UpdateStatus.UnderContractorAdminReview,
                    MediaUrls = new List<string> { "https://res.cloudinary.com/demo/image/upload/road_asphalt_1.jpg" },
                    MediaTypes = new List<string> { "image" },
                    ProgressPercentage = 30, SubmittedAt = DateTime.UtcNow.AddDays(-1),
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                }, ct);
            }

            // Update 3: Task 7 - MRI Room (AdminApproved, full chain completed)
            if (!await _unitOfWork.TaskUpdates.AnyAsync(u => u.TaskId == tasks["Task7"].Id && u.SubmittedByUserId == samuelId, ct))
            {
                await _unitOfWork.TaskUpdates.AddAsync(new TaskUpdate {
                    Id = Guid.NewGuid(), TaskId = tasks["Task7"].Id, SubmittedByUserId = samuelId,
                    Description = "MRI room lead shielding 100% complete. Medical gas lines pressure tested. Oxygen, Nitrous, Suction all verified.",
                    Status = UpdateStatus.AdminApproved,
                    MediaUrls = new List<string> {
                        "https://res.cloudinary.com/demo/image/upload/mri_shielding_1.jpg",
                        "https://res.cloudinary.com/demo/image/upload/medical_gas_1.mp4",
                    },
                    MediaTypes = new List<string> { "image", "video" },
                    ProgressPercentage = 55, SubmittedAt = DateTime.UtcNow.AddDays(-5),
                    SupervisorApproved = true, ReviewedBySupervisorId = users["chloe.west@mailinator.com"].Id,
                    SupervisorReviewedAt = DateTime.UtcNow.AddDays(-4),
                    SupervisorFeedback = "All shielding measurements verified. Approved for admin review.",
                    AdminApproved = true, ReviewedByAdminId = users["michael.chen@mailinator.com"].Id,
                    AdminReviewedAt = DateTime.UtcNow.AddDays(-3),
                    AdminFeedback = "Excellent work. Fully approved.",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                }, ct);
            }

            // Update 4: Task 1 - Column Grid (non-contracted: FieldWorker submitted, awaiting Supervisor)
            var danId = users["dan.foster@mailinator.com"].Id;
            if (!await _unitOfWork.TaskUpdates.AnyAsync(u => u.TaskId == tasks["Task1"].Id && u.SubmittedByUserId == danId, ct))
            {
                await _unitOfWork.TaskUpdates.AddAsync(new TaskUpdate {
                    Id = Guid.NewGuid(), TaskId = tasks["Task1"].Id, SubmittedByUserId = danId,
                    Description = "Column grid Levels 1-3 complete. Steel reinforcement cages erected and concrete poured. Level 4 cages currently being assembled. On track for deadline.",
                    Status = UpdateStatus.UnderSupervisorReview,
                    MediaUrls = new List<string> {
                        "https://res.cloudinary.com/demo/image/upload/column_grid_l1.jpg",
                        "https://res.cloudinary.com/demo/image/upload/column_grid_l3.jpg",
                    },
                    MediaTypes = new List<string> { "image", "image" },
                    ProgressPercentage = 65, SubmittedAt = DateTime.UtcNow.AddDays(-3),
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                }, ct);
            }

            // Update 5: Task 3 - Drainage/Sewer (contracted via AquaTech, awaiting ContractorAdmin review)
            if (!await _unitOfWork.TaskUpdates.AnyAsync(u => u.TaskId == tasks["Task3"].Id && u.SubmittedByUserId == samuelId, ct))
            {
                await _unitOfWork.TaskUpdates.AddAsync(new TaskUpdate {
                    Id = Guid.NewGuid(), TaskId = tasks["Task3"].Id, SubmittedByUserId = samuelId,
                    Description = "Drainage lines installed on floors 1-4. Main sewer stack connected and pressure-tested at 6 bar. Floors 5-10 pending material delivery estimated next week.",
                    Status = UpdateStatus.UnderContractorAdminReview,
                    MediaUrls = new List<string> { "https://res.cloudinary.com/demo/image/upload/drainage_floors_1_4.jpg" },
                    MediaTypes = new List<string> { "image" },
                    ProgressPercentage = 40, SubmittedAt = DateTime.UtcNow.AddDays(-1),
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                }, ct);
            }

            // Update 6: Task 5 - Site Clearing (non-contracted, fully completed chain)
            var davidId = users["david.osei@mailinator.com"].Id;
            if (!await _unitOfWork.TaskUpdates.AnyAsync(u => u.TaskId == tasks["Task5"].Id && u.SubmittedByUserId == danId, ct))
            {
                await _unitOfWork.TaskUpdates.AddAsync(new TaskUpdate {
                    Id = Guid.NewGuid(), TaskId = tasks["Task5"].Id, SubmittedByUserId = danId,
                    Description = "Full site clearing and topographic survey completed. 8.4 km corridor surveyed. All vegetation removed, stakes placed at 50 m intervals. Survey data submitted to design team.",
                    Status = UpdateStatus.AdminApproved,
                    MediaUrls = new List<string> {
                        "https://res.cloudinary.com/demo/image/upload/site_clearing_done.jpg",
                        "https://res.cloudinary.com/demo/image/upload/survey_stakes.jpg",
                    },
                    MediaTypes = new List<string> { "image", "image" },
                    ProgressPercentage = 100, SubmittedAt = DateTime.UtcNow.AddDays(-20),
                    SupervisorApproved = true, ReviewedBySupervisorId = users["ken.park@mailinator.com"].Id,
                    SupervisorReviewedAt = DateTime.UtcNow.AddDays(-18),
                    SupervisorFeedback = "Survey data verified against reference benchmarks. Clearing done to spec. Approved.",
                    AdminApproved = true, ReviewedByAdminId = users["james.okafor@mailinator.com"].Id,
                    AdminReviewedAt = DateTime.UtcNow.AddDays(-17),
                    AdminFeedback = "Excellent work. Survey deliverable accepted. Task marked complete.",
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                }, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded task updates.");
        }

        // ============================================================
        //  TASK COMMENTS
        // ============================================================
        private async Task SeedTaskCommentsAsync(Dictionary<string, ProjectTask> tasks, Dictionary<string, User> users, CancellationToken ct)
        {
            _logger.LogInformation("Seeding task comments...");
            var commentDefs = new[]
            {
                new { TaskKey = "Task1", Email = "tony.adeyemi@mailinator.com",  Text = "Great progress on the column grid. Please ensure all joints are inspected before the concrete pour."    },
                new { TaskKey = "Task2", Email = "ken.park@mailinator.com",       Text = "All wiring follows BS 7671 standard. Certificates will be uploaded by end of week."                    },
                new { TaskKey = "Task7", Email = "michael.chen@mailinator.com",   Text = "Excellent work Samuel. Please coordinate with the hospital engineering team for final sign-off."        },
            };
            foreach (var c in commentDefs)
            {
                var taskId = tasks[c.TaskKey].Id; var userId = users[c.Email].Id;
                if (await _unitOfWork.TaskComments.AnyAsync(tc => tc.TaskId == taskId && tc.UserId == userId && tc.Comment == c.Text, ct)) continue;
                await _unitOfWork.TaskComments.AddAsync(new TaskComment {
                    Id = Guid.NewGuid(), TaskId = taskId, UserId = userId, Comment = c.Text, CreatedAt = DateTime.UtcNow,
                }, ct);
            }
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded task comments.");
        }

        // ============================================================
        //  WORK INFOS
        // ============================================================
        private async Task SeedWorkInfosAsync(
            Dictionary<string, Project>      projects,
            Dictionary<string, Department>   departments,
            Dictionary<string, Contractor>   contractors,
            Dictionary<string, User>         users,
            Dictionary<string, Organization> orgs,
            CancellationToken ct)
        {
            _logger.LogInformation("Seeding work infos...");

            var defs = new (string Email, string ProjKey, string OrgKey, string? DeptKey, string? ContrKey, string Position, DateTime Start, DateTime? End, ContractType ContractType)[]
            {
                // ── Eko Atlantic Tower ─────────────────────────────────────────────────
                ("james.okafor@mailinator.com",  "EkoAtlantic",        "OkaforBuilds",    null,              null,         "Managing Director",              D(2025, 1, 15), D(2027, 6, 30),  ContractType.FullTime),
                ("tony.adeyemi@mailinator.com",  "EkoAtlantic",        "OkaforBuilds",    "StructuralEng",   null,         "Project Administrator",          D(2025, 1, 15), D(2027, 6, 30),  ContractType.FullTime),
                ("rita.obi@mailinator.com",       "EkoAtlantic",        "OkaforBuilds",    "ElectricalSys",   null,         "Electrical Supervisor",          D(2025, 1, 15), D(2027, 6, 30),  ContractType.FullTime),
                ("samuel.king@mailinator.com",   "EkoAtlantic",        "OkaforBuilds",    "PlumbingUtil",    null,         "Plumbing Supervisor",            D(2025, 1, 15), D(2027, 6, 30),  ContractType.FullTime),
                ("amaka.nwosu@mailinator.com",   "EkoAtlantic",        "OkaforBuilds",    "InteriorFin",     null,         "Interior Finishing Supervisor",  D(2025, 1, 15), D(2027, 6, 30),  ContractType.FullTime),
                ("dan.foster@mailinator.com",    "EkoAtlantic",        "OkaforBuilds",    "StructuralEng",   null,         "Structural Technician",          D(2025, 1, 15), D(2027, 6, 30),  ContractType.Contract),
                ("ken.park@mailinator.com",       "EkoAtlantic",        "OkaforBuilds",    "ElectricalSys",   "FastWire",   "Lead Electrical Contractor",    D(2025, 3, 1),  D(2026, 9, 30),  ContractType.Contract),
                ("david.osei@mailinator.com",    "EkoAtlantic",        "OkaforBuilds",    "PlumbingUtil",    "AquaTech",   "Plumbing Contractor Lead",       D(2025, 4, 1),  D(2026, 6, 30),  ContractType.Contract),
                ("chloe.west@mailinator.com",    "EkoAtlantic",        "OkaforBuilds",    "StructuralEng",   null,         "Structural Field Worker",        D(2025, 1, 15), D(2027, 6, 30),  ContractType.Contract),
                // ── Abuja Ring Road Extension ──────────────────────────────────────────
                ("grace.eze@mailinator.com",      "AbujaRingRoad",      "OkaforBuilds",    null,              null,         "Project Owner",                  D(2025, 3, 1),  null,            ContractType.FullTime),
                ("james.okafor@mailinator.com",  "AbujaRingRoad",      "OkaforBuilds",    null,              null,         "Project Administrator",          D(2025, 3, 1),  null,            ContractType.FullTime),
                ("dan.foster@mailinator.com",    "AbujaRingRoad",      "OkaforBuilds",    "FoundationEarth", null,         "Site Supervisor",                D(2025, 3, 1),  null,            ContractType.Contract),
                ("ken.park@mailinator.com",       "AbujaRingRoad",      "OkaforBuilds",    "RoadConstruct",   null,         "Road Construction Supervisor",  D(2025, 3, 1),  null,            ContractType.Contract),
                ("fatima.bello@mailinator.com",  "AbujaRingRoad",      "OkaforBuilds",    "RoadConstruct",   "RoadMaster", "Contractor Admin - RoadMaster",  D(2025, 6, 1),  D(2028, 11, 30), ContractType.Subcontractor),
                ("rita.obi@mailinator.com",       "AbujaRingRoad",      "OkaforBuilds",    "RoadConstruct",   "RoadMaster", "Asphalt Field Worker",           D(2025, 6, 1),  D(2028, 11, 30), ContractType.Subcontractor),
                ("michael.chen@mailinator.com",  "AbujaRingRoad",      "OkaforBuilds",    null,              null,         "Project Observer",               D(2025, 3, 1),  null,            ContractType.FullTime),
                // ── Lagos State Hospital Renovation ────────────────────────────────────
                ("linda.stone@mailinator.com",   "HospitalReno",       "StonePartners",   null,              null,         "Project Owner",                  D(2025, 2, 1),  D(2026, 8, 31),  ContractType.FullTime),
                ("michael.chen@mailinator.com",  "HospitalReno",       "StonePartners",   "CivilWorks",      null,         "Project Administrator",          D(2025, 2, 1),  D(2026, 8, 31),  ContractType.FullTime),
                ("chloe.west@mailinator.com",    "HospitalReno",       "StonePartners",   "MedicalSystems",  null,         "Medical Systems Supervisor",     D(2025, 2, 1),  D(2026, 8, 31),  ContractType.FullTime),
                ("samuel.king@mailinator.com",   "HospitalReno",       "StonePartners",   "MedicalSystems",  null,         "Medical Systems Technician",     D(2025, 2, 1),  D(2026, 8, 31),  ContractType.Contract),
                // ── Presidential Garden Park ───────────────────────────────────────────
                ("superadmin@apexbuild.io",      "PresidentialGarden", "SuperAdminDirect", null,             null,         "Platform Owner",                 D(2025, 6, 1),  null,            ContractType.FullTime),
                ("linda.stone@mailinator.com",   "PresidentialGarden", "SuperAdminDirect", null,             null,         "Project Administrator",          D(2025, 6, 1),  null,            ContractType.FullTime),
            };

            foreach (var w in defs)
            {
                var user = users[w.Email]; var project = projects[w.ProjKey];
                if (await _unitOfWork.WorkInfos.AnyAsync(wi => wi.UserId == user.Id && wi.ProjectId == project.Id && wi.Position == w.Position, ct)) continue;
                await _unitOfWork.WorkInfos.AddAsync(new WorkInfo {
                    Id = Guid.NewGuid(), UserId = user.Id, ProjectId = project.Id,
                    OrganizationId = orgs[w.OrgKey].Id,
                    DepartmentId = w.DeptKey != null ? (Guid?)departments[w.DeptKey].Id : null,
                    ContractorId = w.ContrKey != null ? (Guid?)contractors[w.ContrKey].Id : null,
                    Position = w.Position, StartDate = w.Start, EndDate = w.End,
                    ContractType = w.ContractType, Status = ProjectUserStatus.Active, IsActive = true, CreatedAt = DateTime.UtcNow,
                }, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded work infos.");
        }

        // ============================================================
        //  INVITATIONS
        // ============================================================
        private async Task SeedInvitationsAsync(Dictionary<string, Project> projects, Dictionary<string, User> users, Dictionary<string, Role> roles, CancellationToken ct)
        {
            _logger.LogInformation("Seeding invitations...");

            // Invitation 1: New unregistered member invited to Eko Atlantic Tower
            const string newMemberEmail = "newmember@mailinator.com";
            if (!await _unitOfWork.Invitations.AnyAsync(i => i.Email == newMemberEmail && i.ProjectId == projects["EkoAtlantic"].Id, ct))
            {
                await _unitOfWork.Invitations.AddAsync(new Invitation {
                    Id = Guid.NewGuid(), InvitedByUserId = users["james.okafor@mailinator.com"].Id,
                    Email = newMemberEmail, IsExistingUser = false,
                    RoleId = roles["FieldWorker"].Id, ProjectId = projects["EkoAtlantic"].Id,
                    Token = Guid.NewGuid().ToString("N"), Status = InvitationStatus.Pending,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    Position = "Junior Structural Technician", ContractType = ContractType.Contract,
                    WorkStartDate = D(2026, 1, 1), CreatedAt = DateTime.UtcNow,
                }, ct);
            }

            // Invitation 2: Existing user dan.foster invited to Hospital Renovation as Observer
            var danUser = users["dan.foster@mailinator.com"];
            if (!await _unitOfWork.Invitations.AnyAsync(i => i.InvitedUserId == danUser.Id && i.ProjectId == projects["HospitalReno"].Id, ct))
            {
                await _unitOfWork.Invitations.AddAsync(new Invitation {
                    Id = Guid.NewGuid(), InvitedByUserId = users["linda.stone@mailinator.com"].Id,
                    InvitedUserId = danUser.Id, Email = "dan.foster@mailinator.com",
                    IsExistingUser = true, RoleId = roles["Observer"].Id,
                    ProjectId = projects["HospitalReno"].Id,
                    Token = Guid.NewGuid().ToString("N"), Status = InvitationStatus.Pending,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    Message = "We would love to have your expertise reviewing the civil works.",
                    CreatedAt = DateTime.UtcNow,
                }, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded invitations.");
        }

        // ============================================================
        //  NOTIFICATIONS
        // ============================================================
        private async Task SeedNotificationsAsync(Dictionary<string, User> users, CancellationToken ct)
        {
            _logger.LogInformation("Seeding notifications...");
            var notifDefs = new[]
            {
                new { Email = "tony.adeyemi@mailinator.com",  Type = NotificationType.PendingApproval,      Title = "Task Update Awaiting Your Review",  Message = "The electrical panel installation update needs your review on Eko Atlantic Tower."     },
                new { Email = "ken.park@mailinator.com",      Type = NotificationType.TaskUpdate,           Title = "Your Update Has Been Approved",      Message = "Your progress update on Task 2 has been approved by the Contractor Admin."           },
                new { Email = "james.okafor@mailinator.com",  Type = NotificationType.ContractExpiringSoon, Title = "Contractor Contract Expiring Soon",  Message = "FastWire Electricals contract expires in 14 days. Please review and renew."          },
            };
            foreach (var n in notifDefs)
            {
                var userId = users[n.Email].Id;
                if (await _unitOfWork.Notifications.AnyAsync(x => x.UserId == userId && x.Title == n.Title, ct)) continue;
                await _unitOfWork.Notifications.AddAsync(new Notification {
                    Id = Guid.NewGuid(), UserId = userId, Type = n.Type, Title = n.Title,
                    Message = n.Message, Channel = NotificationChannel.InApp,
                    IsRead = false, CreatedAt = DateTime.UtcNow,
                }, ct);
            }
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded notifications.");
        }

        // ============================================================
        //  CLEAR SEEDED DATA
        // ============================================================
        public async Task<(bool Success, string Message)> ClearSeededDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting clear of seeded data...");

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Delete in reverse dependency order
                var notifications = await _unitOfWork.Notifications.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.Notifications.DeleteRangeAsync(notifications, cancellationToken);

                var invitations = await _unitOfWork.Invitations.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.Invitations.DeleteRangeAsync(invitations, cancellationToken);

                var workInfos = await _unitOfWork.WorkInfos.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.WorkInfos.DeleteRangeAsync(workInfos, cancellationToken);

                var taskComments = await _unitOfWork.TaskComments.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.TaskComments.DeleteRangeAsync(taskComments, cancellationToken);

                var taskUpdates = await _unitOfWork.TaskUpdates.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.TaskUpdates.DeleteRangeAsync(taskUpdates, cancellationToken);

                var tasks = await _unitOfWork.Tasks.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.Tasks.DeleteRangeAsync(tasks, cancellationToken);

                var milestones = await _unitOfWork.Milestones.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.Milestones.DeleteRangeAsync(milestones, cancellationToken);

                var projectUsers = await _unitOfWork.ProjectUsers.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.ProjectUsers.DeleteRangeAsync(projectUsers, cancellationToken);

                var userRoles = await _unitOfWork.UserRoles.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.UserRoles.DeleteRangeAsync(userRoles, cancellationToken);

                var contractors = await _unitOfWork.Contractors.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.Contractors.DeleteRangeAsync(contractors, cancellationToken);

                var departments = await _unitOfWork.Departments.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.Departments.DeleteRangeAsync(departments, cancellationToken);

                var projects = await _unitOfWork.Projects.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.Projects.DeleteRangeAsync(projects, cancellationToken);

                var subscriptions = await _unitOfWork.Subscriptions.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.Subscriptions.DeleteRangeAsync(subscriptions, cancellationToken);

                var orgMembers = await _unitOfWork.OrganizationMembers.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.OrganizationMembers.DeleteRangeAsync(orgMembers, cancellationToken);

                var orgs = await _unitOfWork.Organizations.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.Organizations.DeleteRangeAsync(orgs, cancellationToken);

                var users = await _unitOfWork.Users.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.Users.DeleteRangeAsync(users, cancellationToken);

                var roles = await _unitOfWork.Roles.FindAsync(_ => true, cancellationToken);
                await _unitOfWork.Roles.DeleteRangeAsync(roles, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Seeded data cleared successfully.");
                return (true, "Seeded data cleared successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to clear seeded data.");
                return (false, $"Clear failed: {ex.Message}");
            }
        }

    }
}
