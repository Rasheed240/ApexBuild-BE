using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using Microsoft.Extensions.Logging;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Infrastructure.Data
{
    /// <summary>
    /// Second seed set: 2 projects (Spaceship + Football Stadium) in the SuperAdmin org,
    /// 20 new users, massively populated with tasks, subtasks, reviews, and comments
    /// for thorough end-to-end testing.
    /// </summary>
    public class DatabaseSeeder2
    {
        private static DateTime D(int y, int m, int d) => new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc);

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<DatabaseSeeder2> _logger;

        public DatabaseSeeder2(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            ILogger<DatabaseSeeder2> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> SeedAsync(CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Starting DatabaseSeeder2...");

                // Idempotency guard
                if (await _unitOfWork.Projects.AnyAsync(p => p.Code == "PROJ-2026-001", ct))
                {
                    _logger.LogInformation("DatabaseSeeder2 already run. Skipping.");
                    return (true, "DatabaseSeeder2 already seeded.");
                }

                await _unitOfWork.BeginTransactionAsync(ct);

                // ── Query existing stable data from DB ──────────────────────────────
                var roles = await LoadRolesAsync(ct);
                var superAdminUser = await _unitOfWork.Users.GetByEmailAsync("superadmin@apexbuild.io", ct)
                    ?? throw new Exception("SuperAdmin user not found. Run seed1 first.");
                var superAdminOrg = await _unitOfWork.Organizations.FirstOrDefaultAsync(o => o.Code == "ORG-2025-000", ct)
                    ?? throw new Exception("SuperAdmin org not found. Run seed1 first.");

                // ── New data ────────────────────────────────────────────────────────
                var users     = await SeedUsersAsync(ct);
                await SeedOrgMembersAsync(superAdminOrg, superAdminUser, users, ct);
                var projects  = await SeedProjectsAsync(superAdminOrg, superAdminUser, users, ct);
                var depts     = await SeedDepartmentsAsync(projects, users, ct);
                var contractors = await SeedContractorsAsync(projects, depts, users, ct);
                await LinkDeptContractorsAsync(depts, contractors, ct);
                var milestones = await SeedMilestonesAsync(projects, ct);
                await SeedProjectUsersAsync(projects, users, roles, ct);
                await SeedWorkInfosAsync(projects, depts, contractors, superAdminOrg, users, ct);
                var tasks     = await SeedTasksAsync(projects, depts, contractors, milestones, superAdminUser, users, ct);
                var subtasks  = await SeedSubtasksAsync(tasks, users, ct);
                await SeedTaskUsersAsync(tasks, subtasks, ct);
                await SeedTaskUpdatesAsync(tasks, users, depts, ct);
                await SeedTaskCommentsAsync(tasks, subtasks, users, ct);

                await _unitOfWork.CommitTransactionAsync(ct);
                _logger.LogInformation("DatabaseSeeder2 completed successfully.");
                return (true, "DatabaseSeeder2 seeded successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                _logger.LogError(ex, "DatabaseSeeder2 failed.");
                var sb = new System.Text.StringBuilder(ex.Message);
                var inner = ex.InnerException;
                while (inner != null) { sb.Append(" | INNER: "); sb.Append(inner.Message); inner = inner.InnerException; }
                return (false, $"Seeder2 failed: {sb}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Load existing roles from DB
        // ─────────────────────────────────────────────────────────────────────────
        private async Task<Dictionary<string, Role>> LoadRolesAsync(CancellationToken ct)
        {
            var allRoles = await _unitOfWork.Roles.FindAsync(r => true, ct);
            return allRoles.ToDictionary(r => r.Name);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  20 NEW USERS
        // ─────────────────────────────────────────────────────────────────────────
        private async Task<Dictionary<string, User>> SeedUsersAsync(CancellationToken ct)
        {
            _logger.LogInformation("Seeding 20 new users...");
            var hash = _passwordHasher.HashPassword("Password123%");
            var defs = new[]
            {
                // ── Spaceship team (users 1-15) ───────────────────────────────────
                new { Email = "zara.okonkwo@mailinator.com",    First = "Zara",     Last = "Okonkwo",   Phone = "+2348000000020" }, //  1 Spaceship ProjectOwner
                new { Email = "ibrahim.musa@mailinator.com",    First = "Ibrahim",  Last = "Musa",      Phone = "+2348000000021" }, //  2 Spaceship ProjectAdmin
                new { Email = "elena.novak@mailinator.com",     First = "Elena",    Last = "Novak",     Phone = "+2348000000022" }, //  3 Supervisor – Propulsion (SS) / Seating (Stadium)
                new { Email = "kwame.asante@mailinator.com",    First = "Kwame",    Last = "Asante",    Phone = "+2348000000023" }, //  4 FieldWorker – Propulsion (SS) / Seating (Stadium)
                new { Email = "priya.sharma@mailinator.com",    First = "Priya",    Last = "Sharma",    Phone = "+2348000000024" }, //  5 FieldWorker – Propulsion (SS) / Seating (Stadium)
                new { Email = "felix.wagner@mailinator.com",    First = "Felix",    Last = "Wagner",    Phone = "+2348000000025" }, //  6 ContractorAdmin SpaceTech (SS) / FieldWorker (Stadium)
                new { Email = "aisha.diallo@mailinator.com",    First = "Aisha",    Last = "Diallo",    Phone = "+2348000000026" }, //  7 Supervisor – Avionics (SS) / Electrical (Stadium)
                new { Email = "yusuf.adamu@mailinator.com",     First = "Yusuf",    Last = "Adamu",     Phone = "+2348000000027" }, //  8 FieldWorker – Avionics (SS) / Electrical (Stadium)
                new { Email = "sophie.laurent@mailinator.com",  First = "Sophie",   Last = "Laurent",   Phone = "+2348000000028" }, //  9 FieldWorker – Avionics (SS) / Electrical (Stadium)
                new { Email = "emeka.eze@mailinator.com",       First = "Emeka",    Last = "Eze",       Phone = "+2348000000029" }, // 10 ContractorAdmin AeroSystems (SS) / FieldWorker (Stadium)
                new { Email = "nadia.santos@mailinator.com",    First = "Nadia",    Last = "Santos",    Phone = "+2348000000030" }, // 11 Supervisor – LifeSupport (SS) / Pitch (Stadium)
                new { Email = "roberto.silva@mailinator.com",   First = "Roberto",  Last = "Silva",     Phone = "+2348000000031" }, // 12 FieldWorker – LifeSupport (SS) / Pitch (Stadium)
                new { Email = "chinwe.obiora@mailinator.com",   First = "Chinwe",   Last = "Obiora",    Phone = "+2348000000032" }, // 13 FieldWorker – LifeSupport (SS) / Pitch (Stadium)
                new { Email = "hassan.ali@mailinator.com",      First = "Hassan",   Last = "Ali",       Phone = "+2348000000033" }, // 14 Supervisor – Hull (SS) / FieldWorker Pitch (Stadium)
                new { Email = "mei.lin@mailinator.com",         First = "Mei",      Last = "Lin",       Phone = "+2348000000034" }, // 15 FieldWorker Hull (SS) / ContractorAdmin EcoGrass (Stadium)
                // ── Stadium-only team (users 16-20) ──────────────────────────────
                new { Email = "pascal.dubois@mailinator.com",   First = "Pascal",   Last = "Dubois",    Phone = "+2348000000035" }, // 16 Stadium ProjectAdmin
                new { Email = "adaeze.onuoha@mailinator.com",   First = "Adaeze",   Last = "Onuoha",    Phone = "+2348000000036" }, // 17 Stadium ProjectOwner
                new { Email = "kofi.mensah@mailinator.com",     First = "Kofi",     Last = "Mensah",    Phone = "+2348000000037" }, // 18 Supervisor – Foundation (Stadium)
                new { Email = "sara.berg@mailinator.com",       First = "Sara",     Last = "Berg",      Phone = "+2348000000038" }, // 19 FieldWorker – Foundation (Stadium)
                new { Email = "olumide.adebayo@mailinator.com", First = "Olumide",  Last = "Adebayo",   Phone = "+2348000000039" }, // 20 ContractorAdmin SteelBuild (Stadium)
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

        // ─────────────────────────────────────────────────────────────────────────
        //  ADD ALL 20 USERS TO SUPERADMIN ORG
        // ─────────────────────────────────────────────────────────────────────────
        private async Task SeedOrgMembersAsync(Organization org, User superAdmin, Dictionary<string, User> users, CancellationToken ct)
        {
            _logger.LogInformation("Adding users to SuperAdminDirect org...");
            var positions = new Dictionary<string, string>
            {
                ["zara.okonkwo@mailinator.com"]    = "Aerospace Project Owner",
                ["ibrahim.musa@mailinator.com"]    = "Project Administrator",
                ["elena.novak@mailinator.com"]     = "Department Supervisor",
                ["kwame.asante@mailinator.com"]    = "Field Engineer",
                ["priya.sharma@mailinator.com"]    = "Field Engineer",
                ["felix.wagner@mailinator.com"]    = "Contractor Administrator",
                ["aisha.diallo@mailinator.com"]    = "Department Supervisor",
                ["yusuf.adamu@mailinator.com"]     = "Systems Engineer",
                ["sophie.laurent@mailinator.com"]  = "Systems Engineer",
                ["emeka.eze@mailinator.com"]       = "Contractor Administrator",
                ["nadia.santos@mailinator.com"]    = "Department Supervisor",
                ["roberto.silva@mailinator.com"]   = "Field Engineer",
                ["chinwe.obiora@mailinator.com"]   = "Field Engineer",
                ["hassan.ali@mailinator.com"]      = "Department Supervisor",
                ["mei.lin@mailinator.com"]         = "Contractor Administrator",
                ["pascal.dubois@mailinator.com"]   = "Project Administrator",
                ["adaeze.onuoha@mailinator.com"]   = "Stadium Project Owner",
                ["kofi.mensah@mailinator.com"]     = "Department Supervisor",
                ["sara.berg@mailinator.com"]       = "Site Engineer",
                ["olumide.adebayo@mailinator.com"] = "Contractor Administrator",
            };
            foreach (var (email, position) in positions)
            {
                var user = users[email];
                if (await _unitOfWork.OrganizationMembers.AnyAsync(om => om.OrganizationId == org.Id && om.UserId == user.Id, ct)) continue;
                await _unitOfWork.OrganizationMembers.AddAsync(new OrganizationMember
                {
                    Id = Guid.NewGuid(), OrganizationId = org.Id, UserId = user.Id,
                    Position = position, IsActive = true, JoinedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
                }, ct);
            }
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Added 20 users to SuperAdminDirect org.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  PROJECTS
        // ─────────────────────────────────────────────────────────────────────────
        private async Task<Dictionary<string, Project>> SeedProjectsAsync(
            Organization org, User superAdmin, Dictionary<string, User> users, CancellationToken ct)
        {
            _logger.LogInformation("Seeding 2 new projects...");
            var result = new Dictionary<string, Project>();

            var ss = await _unitOfWork.Projects.FirstOrDefaultAsync(p => p.Code == "PROJ-2026-001", ct);
            if (ss == null)
            {
                ss = new Project
                {
                    Id = Guid.NewGuid(), Name = "Build Spaceship", Code = "PROJ-2026-001",
                    OrganizationId = org.Id,
                    Description = "An ambitious experimental programme to design, build, and test a crewed near-orbit spacecraft from the ground up.",
                    Status = ProjectStatus.Active, ProjectType = ProjectType.Industrial,
                    Location = "Lagos Aerospace Research Campus, Lekki", IsActive = true,
                    StartDate = D(2026, 1, 1), ExpectedEndDate = D(2028, 12, 31),
                    Budget = 12_000_000_000m, Currency = "NGN",
                    ProjectOwnerId = users["zara.okonkwo@mailinator.com"].Id,
                    ProjectAdminId = users["ibrahim.musa@mailinator.com"].Id,
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Projects.AddAsync(ss, ct);
            }
            result["Spaceship"] = ss;

            var stad = await _unitOfWork.Projects.FirstOrDefaultAsync(p => p.Code == "PROJ-2026-002", ct);
            if (stad == null)
            {
                stad = new Project
                {
                    Id = Guid.NewGuid(), Name = "Build Football Stadium", Code = "PROJ-2026-002",
                    OrganizationId = org.Id,
                    Description = "Construction of a modern 60,000-seat football stadium with VIP suites, natural grass pitch, and full LED floodlighting.",
                    Status = ProjectStatus.Active, ProjectType = ProjectType.Other,
                    Location = "Eko Sports Complex, Victoria Island, Lagos", IsActive = true,
                    StartDate = D(2026, 2, 1), ExpectedEndDate = D(2028, 6, 30),
                    Budget = 45_000_000_000m, Currency = "NGN",
                    ProjectOwnerId = users["adaeze.onuoha@mailinator.com"].Id,
                    ProjectAdminId = users["pascal.dubois@mailinator.com"].Id,
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Projects.AddAsync(stad, ct);
            }
            result["Stadium"] = stad;

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded 2 projects.");
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  DEPARTMENTS
        // ─────────────────────────────────────────────────────────────────────────
        private async Task<Dictionary<string, Department>> SeedDepartmentsAsync(
            Dictionary<string, Project> projects, Dictionary<string, User> users, CancellationToken ct)
        {
            _logger.LogInformation("Seeding departments...");
            var result = new Dictionary<string, Department>();

            async Task<Department> E(string key, string code, string name, Guid projId, Guid supId, bool isOut, string spec, DateTime start)
            {
                var dept = await _unitOfWork.Departments.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (dept != null) { result[key] = dept; return dept; }
                dept = new Department
                {
                    Id = Guid.NewGuid(), Name = name, Code = code, ProjectId = projId,
                    SupervisorId = supId, IsOutsourced = isOut, Specialization = spec,
                    Status = DepartmentStatus.Active, IsActive = true, StartDate = start, CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Departments.AddAsync(dept, ct);
                result[key] = dept;
                return dept;
            }

            // ── Spaceship ──────────────────────────────────────────────────────
            var ssId = projects["Spaceship"].Id;
            await E("Propulsion",  "DEPT-PROP-001", "Propulsion Systems",      ssId, users["elena.novak@mailinator.com"].Id,    true,  "Rocket Propulsion",      D(2026, 1, 1));
            await E("Avionics",    "DEPT-AVI-001",  "Avionics & Electronics",  ssId, users["aisha.diallo@mailinator.com"].Id,   true,  "Avionics",               D(2026, 1, 1));
            await E("LifeSupport", "DEPT-LIFE-001", "Life Support Systems",    ssId, users["nadia.santos@mailinator.com"].Id,   false, "Life Support",           D(2026, 1, 1));
            await E("HullFab",     "DEPT-HULL-001", "Hull Fabrication",        ssId, users["hassan.ali@mailinator.com"].Id,     false, "Hull & Structures",      D(2026, 1, 1));

            // ── Stadium ────────────────────────────────────────────────────────
            var stId = projects["Stadium"].Id;
            await E("Foundation",    "DEPT-FDS-001", "Foundation & Structure",  stId, users["kofi.mensah@mailinator.com"].Id,    true,  "Civil & Foundation",     D(2026, 2, 1));
            await E("Seating",       "DEPT-SEA-001", "Seating & Grandstands",   stId, users["elena.novak@mailinator.com"].Id,    false, "Seating Systems",        D(2026, 2, 1));
            await E("ElecLighting",  "DEPT-ELL-001", "Electrical & Lighting",   stId, users["aisha.diallo@mailinator.com"].Id,   false, "Electrical Engineering", D(2026, 2, 1));
            await E("PitchLandscape","DEPT-PIT-001", "Pitch & Landscaping",     stId, users["nadia.santos@mailinator.com"].Id,   true,  "Horticulture & Grounds", D(2026, 2, 1));

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} departments.", result.Count);
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  CONTRACTORS
        // ─────────────────────────────────────────────────────────────────────────
        private async Task<Dictionary<string, Contractor>> SeedContractorsAsync(
            Dictionary<string, Project> projects, Dictionary<string, Department> depts, Dictionary<string, User> users, CancellationToken ct)
        {
            _logger.LogInformation("Seeding contractors...");
            var result = new Dictionary<string, Contractor>();

            async Task<Contractor> E(string key, string code, string name, Guid projId, Guid deptId, Guid adminId, string spec, decimal value, DateTime start, DateTime end)
            {
                var c = await _unitOfWork.Contractors.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (c != null) { result[key] = c; return c; }
                c = new Contractor
                {
                    Id = Guid.NewGuid(), CompanyName = name, Code = code,
                    ProjectId = projId, DepartmentId = deptId, ContractorAdminId = adminId,
                    Specialization = spec, ContractStartDate = start, ContractEndDate = end,
                    ContractValue = value, Currency = "NGN", Status = ContractorStatus.Active, CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Contractors.AddAsync(c, ct);
                result[key] = c;
                return c;
            }

            await E("SpaceTech",  "CONTR-2026-001", "SpaceTech Propulsion Ltd",   projects["Spaceship"].Id, depts["Propulsion"].Id,    users["felix.wagner@mailinator.com"].Id,    "Rocket Propulsion",       3_200_000_000m, D(2026, 2, 1), D(2028, 11, 30));
            await E("AeroSystems","CONTR-2026-002", "AeroSystems Corp",            projects["Spaceship"].Id, depts["Avionics"].Id,      users["emeka.eze@mailinator.com"].Id,       "Avionics & Electronics",  1_800_000_000m, D(2026, 3, 1), D(2028, 9, 30));
            await E("SteelBuild", "CONTR-2026-003", "SteelBuild Inc",              projects["Stadium"].Id,   depts["Foundation"].Id,    users["olumide.adebayo@mailinator.com"].Id, "Civil & Steel Works",    15_000_000_000m, D(2026, 3, 1), D(2027, 12, 31));
            await E("EcoGrass",   "CONTR-2026-004", "EcoGrass Solutions Ltd",      projects["Stadium"].Id,   depts["PitchLandscape"].Id, users["mei.lin@mailinator.com"].Id,        "Turf & Horticulture",     2_200_000_000m, D(2026, 6, 1), D(2028, 3, 31));

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} contractors.", result.Count);
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  LINK DEPARTMENTS → CONTRACTORS
        // ─────────────────────────────────────────────────────────────────────────
        private async Task LinkDeptContractorsAsync(Dictionary<string, Department> depts, Dictionary<string, Contractor> contractors, CancellationToken ct)
        {
            var links = new[] {
                (depts["Propulsion"],    contractors["SpaceTech"].Id),
                (depts["Avionics"],      contractors["AeroSystems"].Id),
                (depts["Foundation"],    contractors["SteelBuild"].Id),
                (depts["PitchLandscape"],contractors["EcoGrass"].Id),
            };
            foreach (var (dept, contractorId) in links)
                if (dept.ContractorId == null) { dept.ContractorId = contractorId; await _unitOfWork.Departments.UpdateAsync(dept, ct); }
            await _unitOfWork.SaveChangesAsync(ct);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  MILESTONES
        // ─────────────────────────────────────────────────────────────────────────
        private async Task<Dictionary<string, ProjectMilestone>> SeedMilestonesAsync(
            Dictionary<string, Project> projects, CancellationToken ct)
        {
            _logger.LogInformation("Seeding milestones...");
            var result = new Dictionary<string, ProjectMilestone>();

            async Task<ProjectMilestone> E(string key, Guid projId, string title, string desc, DateTime due, MilestoneStatus st, decimal prog, int order, DateTime? completedAt = null)
            {
                var m = await _unitOfWork.Milestones.FirstOrDefaultAsync(x => x.ProjectId == projId && x.Title == title, ct);
                if (m != null) { result[key] = m; return m; }
                m = new ProjectMilestone
                {
                    Id = Guid.NewGuid(), Title = title, Description = desc, ProjectId = projId,
                    DueDate = due, CompletedAt = completedAt, Status = st, Progress = prog,
                    OrderIndex = order, CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Milestones.AddAsync(m, ct);
                result[key] = m;
                return m;
            }

            var ssId = projects["Spaceship"].Id;
            await E("SS_Propulsion",   ssId, "Propulsion Design & Prototype",    "All propulsion sub-systems designed and prototype engine test-fired.",   D(2026, 9, 30), MilestoneStatus.InProgress, 45,  1);
            await E("SS_Avionics",     ssId, "Avionics Integration Complete",    "Guidance, navigation, and control systems fully integrated and bench-tested.", D(2026, 12, 31), MilestoneStatus.InProgress, 60, 2);
            await E("SS_LifeSupport",  ssId, "Life Support Testing Passed",      "All life support systems pressure-tested and certified for crewed operation.", D(2027, 4, 30), MilestoneStatus.Upcoming,   0,   3);
            await E("SS_Integration",  ssId, "Full System Integration",          "All modules integrated, pre-launch systems check passed.",                D(2027, 10, 31), MilestoneStatus.Upcoming,  0,   4);

            var stId = projects["Stadium"].Id;
            await E("ST_Foundation",   stId, "Foundation & Structure Complete",  "All piling, ground beams and primary structural steel frame completed.", D(2026, 10, 31), MilestoneStatus.InProgress, 55,  1);
            await E("ST_Seating",      stId, "Seating Installation Complete",    "All grandstand tiers and seating frames fully installed.",                D(2027, 3, 31),  MilestoneStatus.Upcoming,   0,   2);
            await E("ST_Electrical",   stId, "Electrical & Lighting Done",       "Floodlights, PA system, scoreboard and concourse lighting fully installed.", D(2027, 6, 30), MilestoneStatus.InProgress, 25, 3);
            await E("ST_Pitch",        stId, "Pitch & Landscaping Done",         "Natural grass, irrigation, drainage and perimeter landscaping complete.", D(2027, 9, 30), MilestoneStatus.Upcoming,   0,   4);
            await E("ST_Opening",      stId, "Grand Opening Ready",              "Full stadium operational: all snagging resolved, safety certificates issued.", D(2028, 3, 31), MilestoneStatus.Upcoming, 0, 5);

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} milestones.", result.Count);
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  PROJECT USERS  (user ↔ project roles, project-scoped UserRoles)
        // ─────────────────────────────────────────────────────────────────────────
        private async Task SeedProjectUsersAsync(
            Dictionary<string, Project> projects, Dictionary<string, User> users, Dictionary<string, Role> roles, CancellationToken ct)
        {
            _logger.LogInformation("Seeding project users...");

            var assignments = new[]
            {
                // ── Build Spaceship (15 users) ─────────────────────────────────
                new { ProjKey = "Spaceship", Email = "zara.okonkwo@mailinator.com",    RoleKey = "ProjectOwner"         },
                new { ProjKey = "Spaceship", Email = "ibrahim.musa@mailinator.com",    RoleKey = "ProjectAdministrator" },
                new { ProjKey = "Spaceship", Email = "elena.novak@mailinator.com",     RoleKey = "DepartmentSupervisor" },
                new { ProjKey = "Spaceship", Email = "aisha.diallo@mailinator.com",    RoleKey = "DepartmentSupervisor" },
                new { ProjKey = "Spaceship", Email = "nadia.santos@mailinator.com",    RoleKey = "DepartmentSupervisor" },
                new { ProjKey = "Spaceship", Email = "hassan.ali@mailinator.com",      RoleKey = "DepartmentSupervisor" },
                new { ProjKey = "Spaceship", Email = "felix.wagner@mailinator.com",    RoleKey = "ContractorAdmin"      },
                new { ProjKey = "Spaceship", Email = "emeka.eze@mailinator.com",       RoleKey = "ContractorAdmin"      },
                new { ProjKey = "Spaceship", Email = "kwame.asante@mailinator.com",    RoleKey = "FieldWorker"          },
                new { ProjKey = "Spaceship", Email = "priya.sharma@mailinator.com",    RoleKey = "FieldWorker"          },
                new { ProjKey = "Spaceship", Email = "yusuf.adamu@mailinator.com",     RoleKey = "FieldWorker"          },
                new { ProjKey = "Spaceship", Email = "sophie.laurent@mailinator.com",  RoleKey = "FieldWorker"          },
                new { ProjKey = "Spaceship", Email = "roberto.silva@mailinator.com",   RoleKey = "FieldWorker"          },
                new { ProjKey = "Spaceship", Email = "chinwe.obiora@mailinator.com",   RoleKey = "FieldWorker"          },
                new { ProjKey = "Spaceship", Email = "mei.lin@mailinator.com",         RoleKey = "FieldWorker"          },

                // ── Build Football Stadium (all 20) ────────────────────────────
                new { ProjKey = "Stadium", Email = "adaeze.onuoha@mailinator.com",   RoleKey = "ProjectOwner"         },
                new { ProjKey = "Stadium", Email = "pascal.dubois@mailinator.com",   RoleKey = "ProjectAdministrator" },
                new { ProjKey = "Stadium", Email = "kofi.mensah@mailinator.com",     RoleKey = "DepartmentSupervisor" },
                new { ProjKey = "Stadium", Email = "elena.novak@mailinator.com",     RoleKey = "DepartmentSupervisor" },
                new { ProjKey = "Stadium", Email = "aisha.diallo@mailinator.com",    RoleKey = "DepartmentSupervisor" },
                new { ProjKey = "Stadium", Email = "nadia.santos@mailinator.com",    RoleKey = "DepartmentSupervisor" },
                new { ProjKey = "Stadium", Email = "olumide.adebayo@mailinator.com", RoleKey = "ContractorAdmin"      },
                new { ProjKey = "Stadium", Email = "mei.lin@mailinator.com",         RoleKey = "ContractorAdmin"      },
                new { ProjKey = "Stadium", Email = "sara.berg@mailinator.com",       RoleKey = "FieldWorker"          },
                new { ProjKey = "Stadium", Email = "zara.okonkwo@mailinator.com",    RoleKey = "FieldWorker"          },
                new { ProjKey = "Stadium", Email = "ibrahim.musa@mailinator.com",    RoleKey = "FieldWorker"          },
                new { ProjKey = "Stadium", Email = "kwame.asante@mailinator.com",    RoleKey = "FieldWorker"          },
                new { ProjKey = "Stadium", Email = "priya.sharma@mailinator.com",    RoleKey = "FieldWorker"          },
                new { ProjKey = "Stadium", Email = "felix.wagner@mailinator.com",    RoleKey = "FieldWorker"          },
                new { ProjKey = "Stadium", Email = "yusuf.adamu@mailinator.com",     RoleKey = "FieldWorker"          },
                new { ProjKey = "Stadium", Email = "sophie.laurent@mailinator.com",  RoleKey = "FieldWorker"          },
                new { ProjKey = "Stadium", Email = "emeka.eze@mailinator.com",       RoleKey = "FieldWorker"          },
                new { ProjKey = "Stadium", Email = "roberto.silva@mailinator.com",   RoleKey = "FieldWorker"          },
                new { ProjKey = "Stadium", Email = "chinwe.obiora@mailinator.com",   RoleKey = "FieldWorker"          },
                new { ProjKey = "Stadium", Email = "hassan.ali@mailinator.com",      RoleKey = "FieldWorker"          },
            };

            foreach (var a in assignments)
            {
                var project = projects[a.ProjKey]; var user = users[a.Email]; var role = roles[a.RoleKey];
                if (!await _unitOfWork.ProjectUsers.AnyAsync(pu => pu.ProjectId == project.Id && pu.UserId == user.Id, ct))
                    await _unitOfWork.ProjectUsers.AddAsync(new ProjectUser
                    {
                        Id = Guid.NewGuid(), ProjectId = project.Id, UserId = user.Id, RoleId = role.Id,
                        Status = ProjectUserStatus.Active, JoinedAt = DateTime.UtcNow, IsActive = true, CreatedAt = DateTime.UtcNow,
                    }, ct);
                if (!await _unitOfWork.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id && ur.ProjectId == project.Id, ct))
                    await _unitOfWork.UserRoles.AddAsync(new UserRole
                    {
                        Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id, ProjectId = project.Id,
                        IsActive = true, ActivatedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
                    }, ct);
            }
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded project users and project-scoped roles.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  WORK INFOS  (user → project → department → optional contractor)
        // ─────────────────────────────────────────────────────────────────────────
        private async Task SeedWorkInfosAsync(
            Dictionary<string, Project> projects,
            Dictionary<string, Department> depts,
            Dictionary<string, Contractor> contractors,
            Organization org,
            Dictionary<string, User> users,
            CancellationToken ct)
        {
            _logger.LogInformation("Seeding work infos...");

            // (email, projKey, deptKey?, contractorKey?, position, contractType, start, end?)
            var defs = new (string Email, string Proj, string? Dept, string? Contr, string Position, ContractType ContractType, DateTime Start, DateTime? End)[]
            {
                // ── Build Spaceship ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                ("zara.okonkwo@mailinator.com",    "Spaceship", null,          null,          "Aerospace Project Owner",           ContractType.FullTime,      D(2026,1,1),  D(2028,12,31)),
                ("ibrahim.musa@mailinator.com",    "Spaceship", null,          null,          "Project Administrator",              ContractType.FullTime,      D(2026,1,1),  D(2028,12,31)),
                ("elena.novak@mailinator.com",     "Spaceship", "Propulsion",  null,          "Propulsion Systems Supervisor",      ContractType.FullTime,      D(2026,1,1),  D(2028,12,31)),
                ("kwame.asante@mailinator.com",    "Spaceship", "Propulsion",  "SpaceTech",   "Propulsion Engineer",                ContractType.Contract,      D(2026,2,1),  D(2028,11,30)),
                ("priya.sharma@mailinator.com",    "Spaceship", "Propulsion",  "SpaceTech",   "Combustion Specialist",              ContractType.Contract,      D(2026,2,1),  D(2028,11,30)),
                ("felix.wagner@mailinator.com",    "Spaceship", "Propulsion",  "SpaceTech",   "SpaceTech Contractor Admin",         ContractType.Contract,      D(2026,2,1),  D(2028,11,30)),
                ("aisha.diallo@mailinator.com",    "Spaceship", "Avionics",    null,          "Avionics Systems Supervisor",        ContractType.FullTime,      D(2026,1,1),  D(2028,12,31)),
                ("yusuf.adamu@mailinator.com",     "Spaceship", "Avionics",    "AeroSystems", "Navigation Systems Engineer",        ContractType.Contract,      D(2026,3,1),  D(2028,9,30)),
                ("sophie.laurent@mailinator.com",  "Spaceship", "Avionics",    "AeroSystems", "Sensor Integration Engineer",        ContractType.Contract,      D(2026,3,1),  D(2028,9,30)),
                ("emeka.eze@mailinator.com",       "Spaceship", "Avionics",    "AeroSystems", "AeroSystems Contractor Admin",       ContractType.Contract,      D(2026,3,1),  D(2028,9,30)),
                ("nadia.santos@mailinator.com",    "Spaceship", "LifeSupport", null,          "Life Support Systems Supervisor",    ContractType.FullTime,      D(2026,1,1),  D(2028,12,31)),
                ("roberto.silva@mailinator.com",   "Spaceship", "LifeSupport", null,          "Environmental Systems Engineer",     ContractType.FullTime,      D(2026,1,1),  D(2028,12,31)),
                ("chinwe.obiora@mailinator.com",   "Spaceship", "LifeSupport", null,          "Thermal Control Engineer",           ContractType.FullTime,      D(2026,1,1),  D(2028,12,31)),
                ("hassan.ali@mailinator.com",      "Spaceship", "HullFab",     null,          "Hull Fabrication Supervisor",        ContractType.FullTime,      D(2026,1,1),  D(2028,12,31)),
                ("mei.lin@mailinator.com",         "Spaceship", "HullFab",     null,          "Composite Materials Engineer",       ContractType.FullTime,      D(2026,1,1),  D(2028,12,31)),
                // ── Build Football Stadium ────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                ("adaeze.onuoha@mailinator.com",   "Stadium",   null,          null,          "Stadium Project Owner",              ContractType.FullTime,      D(2026,2,1),  D(2028,6,30)),
                ("pascal.dubois@mailinator.com",   "Stadium",   null,          null,          "Project Administrator",              ContractType.FullTime,      D(2026,2,1),  D(2028,6,30)),
                ("kofi.mensah@mailinator.com",     "Stadium",   "Foundation",  null,          "Foundation & Structure Supervisor",  ContractType.FullTime,      D(2026,2,1),  D(2027,12,31)),
                ("sara.berg@mailinator.com",       "Stadium",   "Foundation",  null,          "Site Engineer",                      ContractType.FullTime,      D(2026,2,1),  D(2027,12,31)),
                ("zara.okonkwo@mailinator.com",    "Stadium",   "Foundation",  "SteelBuild",  "Foundation Field Engineer",          ContractType.Subcontractor, D(2026,3,1),  D(2027,12,31)),
                ("ibrahim.musa@mailinator.com",    "Stadium",   "Foundation",  "SteelBuild",  "Civil Works Engineer",               ContractType.Subcontractor, D(2026,3,1),  D(2027,12,31)),
                ("olumide.adebayo@mailinator.com", "Stadium",   "Foundation",  "SteelBuild",  "SteelBuild Contractor Admin",        ContractType.Contract,      D(2026,3,1),  D(2027,12,31)),
                ("elena.novak@mailinator.com",     "Stadium",   "Seating",     null,          "Seating & Grandstands Supervisor",   ContractType.FullTime,      D(2026,2,1),  D(2028,6,30)),
                ("kwame.asante@mailinator.com",    "Stadium",   "Seating",     null,          "Seating Installation Engineer",      ContractType.FullTime,      D(2026,2,1),  D(2028,6,30)),
                ("priya.sharma@mailinator.com",    "Stadium",   "Seating",     "SteelBuild",  "Steel Frame Field Worker",           ContractType.Subcontractor, D(2026,3,1),  D(2027,12,31)),
                ("felix.wagner@mailinator.com",    "Stadium",   "Seating",     "SteelBuild",  "Steel Frame Field Worker",           ContractType.Subcontractor, D(2026,3,1),  D(2027,12,31)),
                ("aisha.diallo@mailinator.com",    "Stadium",   "ElecLighting",null,          "Electrical & Lighting Supervisor",   ContractType.FullTime,      D(2026,2,1),  D(2028,6,30)),
                ("yusuf.adamu@mailinator.com",     "Stadium",   "ElecLighting",null,          "Electrical Engineer",                ContractType.FullTime,      D(2026,2,1),  D(2028,6,30)),
                ("sophie.laurent@mailinator.com",  "Stadium",   "ElecLighting",null,          "Lighting Systems Engineer",          ContractType.FullTime,      D(2026,2,1),  D(2028,6,30)),
                ("emeka.eze@mailinator.com",       "Stadium",   "ElecLighting",null,          "Power Distribution Engineer",        ContractType.FullTime,      D(2026,2,1),  D(2028,6,30)),
                ("nadia.santos@mailinator.com",    "Stadium",   "PitchLandscape",null,        "Pitch & Landscaping Supervisor",     ContractType.FullTime,      D(2026,2,1),  D(2028,6,30)),
                ("roberto.silva@mailinator.com",   "Stadium",   "PitchLandscape","EcoGrass",  "Turf Installation Specialist",       ContractType.Subcontractor, D(2026,6,1),  D(2028,3,31)),
                ("chinwe.obiora@mailinator.com",   "Stadium",   "PitchLandscape","EcoGrass",  "Irrigation Systems Specialist",      ContractType.Subcontractor, D(2026,6,1),  D(2028,3,31)),
                ("hassan.ali@mailinator.com",      "Stadium",   "PitchLandscape","EcoGrass",  "Landscape & Drainage Engineer",      ContractType.Subcontractor, D(2026,6,1),  D(2028,3,31)),
                ("mei.lin@mailinator.com",         "Stadium",   "PitchLandscape","EcoGrass",  "EcoGrass Contractor Admin",          ContractType.Contract,      D(2026,6,1),  D(2028,3,31)),
            };

            foreach (var w in defs)
            {
                var user = users[w.Email]; var project = projects[w.Proj];
                Guid? deptId = w.Dept != null ? depts[w.Dept].Id : (Guid?)null;
                Guid? contrId = w.Contr != null ? contractors[w.Contr].Id : (Guid?)null;
                if (await _unitOfWork.WorkInfos.AnyAsync(wi => wi.UserId == user.Id && wi.ProjectId == project.Id && wi.Position == w.Position, ct)) continue;
                await _unitOfWork.WorkInfos.AddAsync(new WorkInfo
                {
                    Id = Guid.NewGuid(), UserId = user.Id, ProjectId = project.Id,
                    OrganizationId = org.Id, DepartmentId = deptId, ContractorId = contrId,
                    Position = w.Position, StartDate = w.Start, EndDate = w.End,
                    ContractType = w.ContractType, Status = ProjectUserStatus.Active, IsActive = true, CreatedAt = DateTime.UtcNow,
                }, ct);
            }
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded work infos.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  TASKS  (5 per project)
        // ─────────────────────────────────────────────────────────────────────────
        private async Task<Dictionary<string, ProjectTask>> SeedTasksAsync(
            Dictionary<string, Project> projects,
            Dictionary<string, Department> depts,
            Dictionary<string, Contractor> contractors,
            Dictionary<string, ProjectMilestone> milestones,
            User superAdmin,
            Dictionary<string, User> users,
            CancellationToken ct)
        {
            _logger.LogInformation("Seeding tasks...");
            var result = new Dictionary<string, ProjectTask>();

            async Task<ProjectTask> E(string key, string code, ProjectTask t)
            {
                var existing = await _unitOfWork.Tasks.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (existing != null) { result[key] = existing; return existing; }
                await _unitOfWork.Tasks.AddAsync(t, ct);
                result[key] = t;
                return t;
            }

            var ssId = projects["Spaceship"].Id;
            var stId = projects["Stadium"].Id;

            // ─── SPACESHIP TASKS ──────────────────────────────────────────────
            await E("SS1", "SS-2026-001", new ProjectTask
            {
                Id = Guid.NewGuid(), Title = "Rocket Engine Combustion Chamber Design & Prototype",
                Code = "SS-2026-001", ProjectId = ssId,
                Description = "Full design cycle, computational fluid dynamics modelling and physical prototype fabrication of the main rocket engine combustion chamber and nozzle assembly.",
                DepartmentId = depts["Propulsion"].Id, ContractorId = contractors["SpaceTech"].Id,
                MilestoneId = milestones["SS_Propulsion"].Id,
                AssignedToUserId = users["kwame.asante@mailinator.com"].Id, // primary for list display
                AssignedByUserId = users["elena.novak@mailinator.com"].Id,
                Status = TaskStatus.InProgress, Priority = 4, Progress = 45, EstimatedHours = 1200,
                StartDate = D(2026,2,1), DueDate = D(2026,9,15), CreatedAt = DateTime.UtcNow.AddDays(-60),
            });
            await E("SS2", "SS-2026-002", new ProjectTask
            {
                Id = Guid.NewGuid(), Title = "Navigation AI & Guidance System Development",
                Code = "SS-2026-002", ProjectId = ssId,
                Description = "Development of the autonomous navigation artificial intelligence software stack, real-time sensor fusion algorithms, and hardware-in-the-loop testing suite.",
                DepartmentId = depts["Avionics"].Id, ContractorId = contractors["AeroSystems"].Id,
                MilestoneId = milestones["SS_Avionics"].Id,
                AssignedToUserId = users["yusuf.adamu@mailinator.com"].Id,
                AssignedByUserId = users["aisha.diallo@mailinator.com"].Id,
                Status = TaskStatus.InProgress, Priority = 4, Progress = 60, EstimatedHours = 900,
                StartDate = D(2026,3,1), DueDate = D(2026,12,15), CreatedAt = DateTime.UtcNow.AddDays(-45),
            });
            await E("SS3", "SS-2026-003", new ProjectTask
            {
                Id = Guid.NewGuid(), Title = "Oxygen & CO2 Scrubbing System Installation",
                Code = "SS-2026-003", ProjectId = ssId,
                Description = "Procurement, fabrication and installation of the primary oxygen generation and CO2 scrubbing assemblies for the crew module life support bay.",
                DepartmentId = depts["LifeSupport"].Id,
                MilestoneId = milestones["SS_LifeSupport"].Id,
                AssignedToUserId = users["roberto.silva@mailinator.com"].Id,
                AssignedByUserId = users["nadia.santos@mailinator.com"].Id,
                Status = TaskStatus.NotStarted, Priority = 3, Progress = 0, EstimatedHours = 600,
                StartDate = D(2026,10,1), DueDate = D(2027,4,1), CreatedAt = DateTime.UtcNow.AddDays(-20),
            });
            await E("SS4", "SS-2026-004", new ProjectTask
            {
                Id = Guid.NewGuid(), Title = "Titanium Hull Panel Fabrication & Assembly",
                Code = "SS-2026-004", ProjectId = ssId,
                Description = "CNC-precision fabrication of heat-resistant titanium alloy outer hull panels, thermal protection coating application, and structural assembly of all fuselage sections.",
                DepartmentId = depts["HullFab"].Id,
                MilestoneId = milestones["SS_Integration"].Id,
                AssignedToUserId = users["hassan.ali@mailinator.com"].Id,
                AssignedByUserId = users["hassan.ali@mailinator.com"].Id,
                Status = TaskStatus.InProgress, Priority = 3, Progress = 30, EstimatedHours = 1800,
                StartDate = D(2026,2,15), DueDate = D(2027,3,31), CreatedAt = DateTime.UtcNow.AddDays(-55),
            });
            await E("SS5", "SS-2026-005", new ProjectTask
            {
                Id = Guid.NewGuid(), Title = "Primary Power Distribution Network Integration",
                Code = "SS-2026-005", ProjectId = ssId,
                Description = "Design, assembly and testing of the spacecraft primary electrical bus, redundant power distribution units, solar array charge controllers and battery management systems.",
                DepartmentId = depts["Avionics"].Id, ContractorId = contractors["AeroSystems"].Id,
                MilestoneId = milestones["SS_Avionics"].Id,
                AssignedToUserId = users["yusuf.adamu@mailinator.com"].Id,
                AssignedByUserId = users["aisha.diallo@mailinator.com"].Id,
                Status = TaskStatus.UnderReview, Priority = 3, Progress = 80, EstimatedHours = 480,
                StartDate = D(2026,3,1), DueDate = D(2026,10,31), CreatedAt = DateTime.UtcNow.AddDays(-50),
            });

            // ─── STADIUM TASKS ───────────────────────────────────────────────
            await E("ST1", "ST-2026-001", new ProjectTask
            {
                Id = Guid.NewGuid(), Title = "Deep Foundation Pile Driving & Concrete Base Slab",
                Code = "ST-2026-001", ProjectId = stId,
                Description = "Complete rotary bored piling to 35 m depth across all four stand zones, followed by installation of the ground beam grid and pour of the 900 mm thick reinforced concrete base slab.",
                DepartmentId = depts["Foundation"].Id, ContractorId = contractors["SteelBuild"].Id,
                MilestoneId = milestones["ST_Foundation"].Id,
                AssignedToUserId = users["sara.berg@mailinator.com"].Id,
                AssignedByUserId = users["kofi.mensah@mailinator.com"].Id,
                Status = TaskStatus.InProgress, Priority = 4, Progress = 55, EstimatedHours = 3600,
                StartDate = D(2026,3,1), DueDate = D(2026,10,15), CreatedAt = DateTime.UtcNow.AddDays(-80),
            });
            await E("ST2", "ST-2026-002", new ProjectTask
            {
                Id = Guid.NewGuid(), Title = "Upper Tier Seating Frame Installation",
                Code = "ST-2026-002", ProjectId = stId,
                Description = "Fabrication and structural installation of all upper tier steel seating frames and terracing risers for Stands B, C and D, totalling approximately 28,000 seat positions.",
                DepartmentId = depts["Seating"].Id,
                MilestoneId = milestones["ST_Seating"].Id,
                AssignedToUserId = users["kwame.asante@mailinator.com"].Id,
                AssignedByUserId = users["elena.novak@mailinator.com"].Id,
                Status = TaskStatus.NotStarted, Priority = 3, Progress = 0, EstimatedHours = 2400,
                StartDate = D(2026,11,1), DueDate = D(2027,3,15), CreatedAt = DateTime.UtcNow.AddDays(-30),
            });
            await E("ST3", "ST-2026-003", new ProjectTask
            {
                Id = Guid.NewGuid(), Title = "Stadium Floodlight Mast Installation & Electrical Grid",
                Code = "ST-2026-003", ProjectId = stId,
                Description = "Erect four 55 m floodlight masts, install 400+ LED luminaires, cable trays, switchgear and the full stadium electrical distribution grid to UEFA Category 4 specification.",
                DepartmentId = depts["ElecLighting"].Id,
                MilestoneId = milestones["ST_Electrical"].Id,
                AssignedToUserId = users["yusuf.adamu@mailinator.com"].Id,
                AssignedByUserId = users["aisha.diallo@mailinator.com"].Id,
                Status = TaskStatus.InProgress, Priority = 4, Progress = 25, EstimatedHours = 1800,
                StartDate = D(2026,4,1), DueDate = D(2027,6,15), CreatedAt = DateTime.UtcNow.AddDays(-60),
            });
            await E("ST4", "ST-2026-004", new ProjectTask
            {
                Id = Guid.NewGuid(), Title = "Natural Grass Pitch Laying & Irrigation System",
                Code = "ST-2026-004", ProjectId = stId,
                Description = "Installation of full stadium drainage, sub-base compaction, sand rootzone mix, hybrid natural grass pitch and automated in-ground irrigation system.",
                DepartmentId = depts["PitchLandscape"].Id, ContractorId = contractors["EcoGrass"].Id,
                MilestoneId = milestones["ST_Pitch"].Id,
                AssignedToUserId = users["roberto.silva@mailinator.com"].Id,
                AssignedByUserId = users["nadia.santos@mailinator.com"].Id,
                Status = TaskStatus.NotStarted, Priority = 3, Progress = 0, EstimatedHours = 1200,
                StartDate = D(2026,10,1), DueDate = D(2027,9,15), CreatedAt = DateTime.UtcNow.AddDays(-15),
            });
            await E("ST5", "ST-2026-005", new ProjectTask
            {
                Id = Guid.NewGuid(), Title = "VIP Box Suites & Premium Hospitality Fit-Out",
                Code = "ST-2026-005", ProjectId = stId,
                Description = "Full design and fit-out of 24 premium VIP hospitality boxes and the 2,000-seat Club Level including bespoke seating, climate control, AV systems and catering infrastructure.",
                DepartmentId = depts["Seating"].Id,
                MilestoneId = milestones["ST_Seating"].Id,
                AssignedToUserId = users["kwame.asante@mailinator.com"].Id,
                AssignedByUserId = users["elena.novak@mailinator.com"].Id,
                Status = TaskStatus.UnderReview, Priority = 3, Progress = 75, EstimatedHours = 960,
                StartDate = D(2026,3,1), DueDate = D(2026,12,31), CreatedAt = DateTime.UtcNow.AddDays(-70),
            });

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} tasks.", result.Count);
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  SUBTASKS
        // ─────────────────────────────────────────────────────────────────────────
        private async Task<Dictionary<string, ProjectTask>> SeedSubtasksAsync(
            Dictionary<string, ProjectTask> tasks, Dictionary<string, User> users, CancellationToken ct)
        {
            _logger.LogInformation("Seeding subtasks...");
            var result = new Dictionary<string, ProjectTask>();

            async Task<ProjectTask> E(string key, string code, ProjectTask t)
            {
                var existing = await _unitOfWork.Tasks.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (existing != null) { result[key] = existing; return existing; }
                await _unitOfWork.Tasks.AddAsync(t, ct);
                result[key] = t;
                return t;
            }

            var ss1 = tasks["SS1"]; var ss2 = tasks["SS2"]; var ss3 = tasks["SS3"];
            var st1 = tasks["ST1"]; var st3 = tasks["ST3"]; var st5 = tasks["ST5"];

            // ─── SS1 Subtasks ─────────────────────────────────────────────────
            await E("SS1_A", "SS-2026-001-A", new ProjectTask {
                Id = Guid.NewGuid(), Title = "CFD Combustion Chamber Modelling",
                Code = "SS-2026-001-A", ProjectId = ss1.ProjectId, DepartmentId = ss1.DepartmentId,
                ContractorId = ss1.ContractorId, ParentTaskId = ss1.Id,
                Description = "Multi-physics CFD analysis of combustion geometry, pressure distribution and thermal loads using ANSYS Fluent.",
                AssignedToUserId = users["kwame.asante@mailinator.com"].Id,
                AssignedByUserId = users["elena.novak@mailinator.com"].Id,
                Status = TaskStatus.Completed, Priority = 4, Progress = 100,
                CompletedAt = DateTime.UtcNow.AddDays(-20), CreatedAt = DateTime.UtcNow.AddDays(-50),
            });
            await E("SS1_B", "SS-2026-001-B", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Prototype Nozzle CNC Fabrication",
                Code = "SS-2026-001-B", ProjectId = ss1.ProjectId, DepartmentId = ss1.DepartmentId,
                ContractorId = ss1.ContractorId, ParentTaskId = ss1.Id,
                Description = "CNC machining of the Inconel 718 prototype rocket nozzle from CFD-validated geometry files.",
                AssignedToUserId = users["priya.sharma@mailinator.com"].Id,
                AssignedByUserId = users["elena.novak@mailinator.com"].Id,
                Status = TaskStatus.InProgress, Priority = 4, Progress = 55, EstimatedHours = 300,
                StartDate = D(2026,5,1), DueDate = D(2026,8,15), CreatedAt = DateTime.UtcNow.AddDays(-40),
            });
            await E("SS1_C", "SS-2026-001-C", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Static Fire Test – Phase 1 (5-second burn)",
                Code = "SS-2026-001-C", ProjectId = ss1.ProjectId, DepartmentId = ss1.DepartmentId,
                ContractorId = ss1.ContractorId, ParentTaskId = ss1.Id,
                Description = "Controlled 5-second static fire test of the prototype engine on the test stand. Measure thrust, chamber pressure and exhaust temperature.",
                AssignedToUserId = users["kwame.asante@mailinator.com"].Id, // primary
                AssignedByUserId = users["elena.novak@mailinator.com"].Id,
                Status = TaskStatus.NotStarted, Priority = 4, Progress = 0, EstimatedHours = 160,
                StartDate = D(2026,8,20), DueDate = D(2026,9,10), CreatedAt = DateTime.UtcNow.AddDays(-30),
            });

            // ─── SS2 Subtasks ─────────────────────────────────────────────────
            await E("SS2_A", "SS-2026-002-A", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Navigation Algorithm Core Module",
                Code = "SS-2026-002-A", ProjectId = ss2.ProjectId, DepartmentId = ss2.DepartmentId,
                ContractorId = ss2.ContractorId, ParentTaskId = ss2.Id,
                Description = "Implementation of the Kalman filter-based inertial navigation core, star tracker interface and orbital mechanics solver.",
                AssignedToUserId = users["yusuf.adamu@mailinator.com"].Id,
                AssignedByUserId = users["aisha.diallo@mailinator.com"].Id,
                Status = TaskStatus.Completed, Priority = 4, Progress = 100,
                CompletedAt = DateTime.UtcNow.AddDays(-15), CreatedAt = DateTime.UtcNow.AddDays(-40),
            });
            await E("SS2_B", "SS-2026-002-B", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Multi-Sensor Array Integration & HITL Testing",
                Code = "SS-2026-002-B", ProjectId = ss2.ProjectId, DepartmentId = ss2.DepartmentId,
                ContractorId = ss2.ContractorId, ParentTaskId = ss2.Id,
                Description = "Integration of IMU, GPS, star tracker and radar altimeter data streams. Hardware-in-the-loop simulation on the Simulink test bench.",
                AssignedToUserId = users["sophie.laurent@mailinator.com"].Id,
                AssignedByUserId = users["aisha.diallo@mailinator.com"].Id,
                Status = TaskStatus.InProgress, Priority = 4, Progress = 40, EstimatedHours = 280,
                StartDate = D(2026,6,1), DueDate = D(2026,10,30), CreatedAt = DateTime.UtcNow.AddDays(-30),
            });

            // ─── SS3 Subtasks ─────────────────────────────────────────────────
            await E("SS3_A", "SS-2026-003-A", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Electrolysis Oxygen Generator Procurement",
                Code = "SS-2026-003-A", ProjectId = ss3.ProjectId, DepartmentId = ss3.DepartmentId,
                ParentTaskId = ss3.Id,
                Description = "Source and qualify PEM electrolysis cell stacks rated at 2 kg O2/day for the crew module bay.",
                AssignedToUserId = users["roberto.silva@mailinator.com"].Id,
                AssignedByUserId = users["nadia.santos@mailinator.com"].Id,
                Status = TaskStatus.NotStarted, Priority = 3, Progress = 0, EstimatedHours = 120,
                StartDate = D(2026,10,1), DueDate = D(2026,12,31), CreatedAt = DateTime.UtcNow.AddDays(-10),
            });

            // ─── ST1 Subtasks ─────────────────────────────────────────────────
            await E("ST1_A", "ST-2026-001-A", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Site Survey & Geotechnical Investigation",
                Code = "ST-2026-001-A", ProjectId = st1.ProjectId, DepartmentId = st1.DepartmentId,
                ContractorId = st1.ContractorId, ParentTaskId = st1.Id,
                Description = "Full topographic survey, trial pit excavations and borehole investigation to 40 m depth across the 12-hectare site.",
                AssignedToUserId = users["sara.berg@mailinator.com"].Id,
                AssignedByUserId = users["kofi.mensah@mailinator.com"].Id,
                Status = TaskStatus.Completed, Priority = 4, Progress = 100,
                CompletedAt = DateTime.UtcNow.AddDays(-40), CreatedAt = DateTime.UtcNow.AddDays(-70),
            });
            await E("ST1_B", "ST-2026-001-B", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Pile Driving – East & West Stand Zones",
                Code = "ST-2026-001-B", ProjectId = st1.ProjectId, DepartmentId = st1.DepartmentId,
                ContractorId = st1.ContractorId, ParentTaskId = st1.Id,
                Description = "Rotary bored piling to 35 m for the East and West stand foundations. 480 piles at 900 mm diameter, C40 concrete.",
                AssignedToUserId = users["zara.okonkwo@mailinator.com"].Id,
                AssignedByUserId = users["kofi.mensah@mailinator.com"].Id,
                Status = TaskStatus.InProgress, Priority = 4, Progress = 70, EstimatedHours = 1200,
                StartDate = D(2026,4,1), DueDate = D(2026,8,31), CreatedAt = DateTime.UtcNow.AddDays(-60),
            });
            await E("ST1_C", "ST-2026-001-C", new ProjectTask {
                Id = Guid.NewGuid(), Title = "North & South Stand Base Slab Concrete Pour",
                Code = "ST-2026-001-C", ProjectId = st1.ProjectId, DepartmentId = st1.DepartmentId,
                ContractorId = st1.ContractorId, ParentTaskId = st1.Id,
                Description = "900 mm thick reinforced concrete base slab pour for the North and South stands. 14,000 m³ C40/50 concrete, post-tension cables.",
                AssignedToUserId = users["sara.berg@mailinator.com"].Id, // primary
                AssignedByUserId = users["kofi.mensah@mailinator.com"].Id,
                Status = TaskStatus.NotStarted, Priority = 4, Progress = 0, EstimatedHours = 960,
                StartDate = D(2026,9,1), DueDate = D(2026,10,10), CreatedAt = DateTime.UtcNow.AddDays(-20),
            });

            // ─── ST3 Subtasks ─────────────────────────────────────────────────
            await E("ST3_A", "ST-2026-003-A", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Floodlight Mast Base Foundations & Erection",
                Code = "ST-2026-003-A", ProjectId = st3.ProjectId, DepartmentId = st3.DepartmentId,
                ParentTaskId = st3.Id,
                Description = "Excavation and pour of 4.5 m diameter circular raft foundations for each of the four 55 m masts, followed by structural erection and tensioning.",
                AssignedToUserId = users["yusuf.adamu@mailinator.com"].Id,
                AssignedByUserId = users["aisha.diallo@mailinator.com"].Id,
                Status = TaskStatus.InProgress, Priority = 4, Progress = 50, EstimatedHours = 480,
                StartDate = D(2026,4,1), DueDate = D(2026,8,31), CreatedAt = DateTime.UtcNow.AddDays(-50),
            });
            await E("ST3_B", "ST-2026-003-B", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Cable Tray Installation & Power Cable Pulling",
                Code = "ST-2026-003-B", ProjectId = st3.ProjectId, DepartmentId = st3.DepartmentId,
                ParentTaskId = st3.Id,
                Description = "Installation of 85 km of cable tray routes throughout all concourse levels, followed by pulling and termination of HV, LV and data cables.",
                AssignedToUserId = users["sophie.laurent@mailinator.com"].Id,
                AssignedByUserId = users["aisha.diallo@mailinator.com"].Id,
                Status = TaskStatus.NotStarted, Priority = 3, Progress = 0, EstimatedHours = 720,
                StartDate = D(2026,9,1), DueDate = D(2027,4,30), CreatedAt = DateTime.UtcNow.AddDays(-30),
            });

            // ─── ST5 Subtasks ─────────────────────────────────────────────────
            await E("ST5_A", "ST-2026-005-A", new ProjectTask {
                Id = Guid.NewGuid(), Title = "VIP Box Shell-out & M&E First Fix",
                Code = "ST-2026-005-A", ProjectId = st5.ProjectId, DepartmentId = st5.DepartmentId,
                ParentTaskId = st5.Id,
                Description = "Blockwork partitions, suspended ceilings, first-fix M&E installations and structural glazing for all 24 VIP boxes.",
                AssignedToUserId = users["kwame.asante@mailinator.com"].Id,
                AssignedByUserId = users["elena.novak@mailinator.com"].Id,
                Status = TaskStatus.Completed, Priority = 3, Progress = 100,
                CompletedAt = DateTime.UtcNow.AddDays(-10), CreatedAt = DateTime.UtcNow.AddDays(-60),
            });
            await E("ST5_B", "ST-2026-005-B", new ProjectTask {
                Id = Guid.NewGuid(), Title = "Premium AV System & Climate Control Installation",
                Code = "ST-2026-005-B", ProjectId = st5.ProjectId, DepartmentId = st5.DepartmentId,
                ParentTaskId = st5.Id,
                Description = "Installation of 4K display systems, premium sound, individual zone climate control units and automated blind systems in all VIP areas.",
                AssignedToUserId = users["felix.wagner@mailinator.com"].Id,
                AssignedByUserId = users["elena.novak@mailinator.com"].Id,
                Status = TaskStatus.InProgress, Priority = 3, Progress = 70, EstimatedHours = 320,
                StartDate = D(2026,7,1), DueDate = D(2026,11,30), CreatedAt = DateTime.UtcNow.AddDays(-50),
            });

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} subtasks.", result.Count);
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  TASK USERS  (multi-assignee for tasks and subtasks)
        // ─────────────────────────────────────────────────────────────────────────
        private async Task SeedTaskUsersAsync(
            Dictionary<string, ProjectTask> tasks,
            Dictionary<string, ProjectTask> subtasks,
            CancellationToken ct)
        {
            _logger.LogInformation("Seeding task user assignments...");

            // (taskKey, userId, isPrimary) — primary = already set as AssignedToUserId on task
            // Subtask assignees must be a subset of parent task's assignees
            var taskAssignments = new (string Key, string Email, bool IsPrimary, bool IsSubtask)[]
            {
                // ── SS1: kwame.asante (primary) + priya.sharma ──────────────────
                ("SS1",   "kwame.asante@mailinator.com",   true,  false),
                ("SS1",   "priya.sharma@mailinator.com",   false, false),
                // ── SS2: yusuf (primary) + sophie + emeka ───────────────────────
                ("SS2",   "yusuf.adamu@mailinator.com",    true,  false),
                ("SS2",   "sophie.laurent@mailinator.com", false, false),
                ("SS2",   "emeka.eze@mailinator.com",      false, false),
                // ── SS3: roberto (primary) + chinwe ─────────────────────────────
                ("SS3",   "roberto.silva@mailinator.com",  true,  false),
                ("SS3",   "chinwe.obiora@mailinator.com",  false, false),
                // ── SS4: hassan (primary) + mei.lin ─────────────────────────────
                ("SS4",   "hassan.ali@mailinator.com",     true,  false),
                ("SS4",   "mei.lin@mailinator.com",        false, false),
                // ── SS5: yusuf (primary) + emeka ────────────────────────────────
                ("SS5",   "yusuf.adamu@mailinator.com",    true,  false),
                ("SS5",   "emeka.eze@mailinator.com",      false, false),
                // ── ST1: sara (primary) + zara + ibrahim ────────────────────────
                ("ST1",   "sara.berg@mailinator.com",      true,  false),
                ("ST1",   "zara.okonkwo@mailinator.com",   false, false),
                ("ST1",   "ibrahim.musa@mailinator.com",   false, false),
                // ── ST2: kwame (primary) + priya ────────────────────────────────
                ("ST2",   "kwame.asante@mailinator.com",   true,  false),
                ("ST2",   "priya.sharma@mailinator.com",   false, false),
                // ── ST3: yusuf (primary) + sophie + emeka ───────────────────────
                ("ST3",   "yusuf.adamu@mailinator.com",    true,  false),
                ("ST3",   "sophie.laurent@mailinator.com", false, false),
                ("ST3",   "emeka.eze@mailinator.com",      false, false),
                // ── ST4: roberto (primary) + chinwe + hassan ────────────────────
                ("ST4",   "roberto.silva@mailinator.com",  true,  false),
                ("ST4",   "chinwe.obiora@mailinator.com",  false, false),
                ("ST4",   "hassan.ali@mailinator.com",     false, false),
                // ── ST5: kwame (primary) + felix ────────────────────────────────
                ("ST5",   "kwame.asante@mailinator.com",   true,  false),
                ("ST5",   "felix.wagner@mailinator.com",   false, false),
                // ── SS1_A: kwame ─────────────────────────────────────────────────
                ("SS1_A", "kwame.asante@mailinator.com",   true,  true),
                // ── SS1_B: priya ─────────────────────────────────────────────────
                ("SS1_B", "priya.sharma@mailinator.com",   true,  true),
                // ── SS1_C: kwame (primary) + priya ───────────────────────────────
                ("SS1_C", "kwame.asante@mailinator.com",   true,  true),
                ("SS1_C", "priya.sharma@mailinator.com",   false, true),
                // ── SS2_A: yusuf ─────────────────────────────────────────────────
                ("SS2_A", "yusuf.adamu@mailinator.com",    true,  true),
                // ── SS2_B: sophie ────────────────────────────────────────────────
                ("SS2_B", "sophie.laurent@mailinator.com", true,  true),
                // ── SS3_A: roberto ───────────────────────────────────────────────
                ("SS3_A", "roberto.silva@mailinator.com",  true,  true),
                // ── ST1_A: sara ──────────────────────────────────────────────────
                ("ST1_A", "sara.berg@mailinator.com",      true,  true),
                // ── ST1_B: zara ──────────────────────────────────────────────────
                ("ST1_B", "zara.okonkwo@mailinator.com",   true,  true),
                // ── ST1_C: sara (primary) + ibrahim ──────────────────────────────
                ("ST1_C", "sara.berg@mailinator.com",      true,  true),
                ("ST1_C", "ibrahim.musa@mailinator.com",   false, true),
                // ── ST3_A: yusuf ─────────────────────────────────────────────────
                ("ST3_A", "yusuf.adamu@mailinator.com",    true,  true),
                // ── ST3_B: sophie ────────────────────────────────────────────────
                ("ST3_B", "sophie.laurent@mailinator.com", true,  true),
                // ── ST5_A: kwame ─────────────────────────────────────────────────
                ("ST5_A", "kwame.asante@mailinator.com",   true,  true),
                // ── ST5_B: felix ─────────────────────────────────────────────────
                ("ST5_B", "felix.wagner@mailinator.com",   true,  true),
            };

            // Build a lookup: merge tasks + subtasks
            var all = tasks.ToDictionary(kv => kv.Key, kv => kv.Value);
            foreach (var kv in subtasks) all[kv.Key] = kv.Value;

            // Also resolve user IDs at runtime from the DB
            var userLookup = new Dictionary<string, User>();
            var emails = taskAssignments.Select(a => a.Email).Distinct().ToList();
            foreach (var email in emails)
            {
                var u = await _unitOfWork.Users.GetByEmailAsync(email, ct);
                if (u != null) userLookup[email] = u;
            }

            foreach (var (key, email, _, _) in taskAssignments)
            {
                if (!all.TryGetValue(key, out var task)) continue;
                if (!userLookup.TryGetValue(email, out var user)) continue;

                if (await _unitOfWork.TaskUsers.AnyAsync(tu => tu.TaskId == task.Id && tu.UserId == user.Id, ct)) continue;

                await _unitOfWork.TaskUsers.AddAsync(new TaskUser
                {
                    Id = Guid.NewGuid(), TaskId = task.Id, UserId = user.Id,
                    AssignedByUserId = task.AssignedByUserId,
                    Role = "Assignee", IsActive = true, AssignedAt = task.CreatedAt,
                }, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded task user assignments.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  TASK UPDATES  (active reviews at various chain stages)
        // ─────────────────────────────────────────────────────────────────────────
        private async Task SeedTaskUpdatesAsync(
            Dictionary<string, ProjectTask> tasks,
            Dictionary<string, User> users,
            Dictionary<string, Department> depts,
            CancellationToken ct)
        {
            _logger.LogInformation("Seeding task updates...");

            // Helper
            async Task AddUpdate(Guid taskId, Guid submittedById, string desc, UpdateStatus status,
                int progress, DateTime submittedAt,
                Guid? contractorAdminReviewerId = null, string? contractorFeedback = null, bool contractorApproved = false,
                Guid? supervisorReviewerId = null, string? supFeedback = null, bool supApproved = false,
                Guid? adminReviewerId = null, string? adminFeedback = null, bool adminApproved = false,
                List<string>? mediaUrls = null, List<string>? mediaTypes = null)
            {
                if (await _unitOfWork.TaskUpdates.AnyAsync(u => u.TaskId == taskId && u.SubmittedByUserId == submittedById && u.Status == status, ct)) return;
                await _unitOfWork.TaskUpdates.AddAsync(new TaskUpdate
                {
                    Id = Guid.NewGuid(), TaskId = taskId, SubmittedByUserId = submittedById,
                    Description = desc, Status = status, ProgressPercentage = progress,
                    SubmittedAt = submittedAt, CreatedAt = submittedAt,
                    MediaUrls = mediaUrls ?? new List<string>(), MediaTypes = mediaTypes ?? new List<string>(),
                    // Contractor admin review
                    ContractorAdminApproved = contractorApproved,
                    ReviewedByContractorAdminId = contractorAdminReviewerId,
                    ContractorAdminReviewedAt = contractorApproved || contractorFeedback != null ? submittedAt.AddDays(1) : (DateTime?)null,
                    ContractorAdminFeedback = contractorFeedback,
                    // Supervisor review
                    SupervisorApproved = supApproved,
                    ReviewedBySupervisorId = supervisorReviewerId,
                    SupervisorReviewedAt = supApproved || supFeedback != null ? submittedAt.AddDays(2) : (DateTime?)null,
                    SupervisorFeedback = supFeedback,
                    // Admin review
                    AdminApproved = adminApproved,
                    ReviewedByAdminId = adminReviewerId,
                    AdminReviewedAt = adminApproved || adminFeedback != null ? submittedAt.AddDays(3) : (DateTime?)null,
                    AdminFeedback = adminFeedback,
                }, ct);
            }

            // ── SS1: kwame submitted → UnderContractorAdminReview (SpaceTech → felix) ──
            await AddUpdate(tasks["SS1"].Id, users["kwame.asante@mailinator.com"].Id,
                "CFD modelling complete. Nozzle prototype machining at 55%. Pressure ratings exceed spec by 8%. Requesting ContractorAdmin sign-off to proceed to static fire.",
                UpdateStatus.UnderContractorAdminReview, 45, DateTime.UtcNow.AddDays(-3),
                mediaUrls: new List<string> { "https://res.cloudinary.com/demo/image/upload/cfd_combustion_1.jpg", "https://res.cloudinary.com/demo/image/upload/nozzle_machining.jpg" },
                mediaTypes: new List<string> { "image", "image" });

            // ── SS2: yusuf submitted → ContractorAdmin (emeka) approved → UnderSupervisorReview ──
            await AddUpdate(tasks["SS2"].Id, users["yusuf.adamu@mailinator.com"].Id,
                "Navigation algorithm core module 100% complete and unit-tested. Sensor array HITL at 40%. Guidance simulation achieving <0.5 km orbital insertion error.",
                UpdateStatus.UnderSupervisorReview, 60, DateTime.UtcNow.AddDays(-5),
                contractorAdminReviewerId: users["emeka.eze@mailinator.com"].Id,
                contractorFeedback: "Algorithm performance verified against mission requirements. Sensor integration on track. Approved for supervisor review.",
                contractorApproved: true,
                mediaUrls: new List<string> { "https://res.cloudinary.com/demo/image/upload/nav_sim_results.jpg", "https://res.cloudinary.com/demo/image/upload/hitl_bench.jpg" },
                mediaTypes: new List<string> { "image", "image" });

            // ── SS4: hassan submitted → UnderSupervisorReview (no contractor) ──
            await AddUpdate(tasks["SS4"].Id, users["hassan.ali@mailinator.com"].Id,
                "Forward fuselage panels 4 of 12 complete. Aft section panels 2 of 8 complete. Thermal protection coating application starting next week. On schedule.",
                UpdateStatus.UnderSupervisorReview, 30, DateTime.UtcNow.AddDays(-2),
                mediaUrls: new List<string> { "https://res.cloudinary.com/demo/image/upload/hull_panel_forward.jpg" },
                mediaTypes: new List<string> { "image" });

            // ── SS5: yusuf submitted → ContractorAdmin approved → Supervisor approved → AdminApproved ──
            await AddUpdate(tasks["SS5"].Id, users["yusuf.adamu@mailinator.com"].Id,
                "Primary power bus installed and load-tested at 120% capacity. Battery management system integrated. Solar array charge controllers calibrated. Ready for system-level test.",
                UpdateStatus.AdminApproved, 80, DateTime.UtcNow.AddDays(-8),
                contractorAdminReviewerId: users["emeka.eze@mailinator.com"].Id,
                contractorFeedback: "All power systems meet spec. Load test data attached. Approved.",
                contractorApproved: true,
                supervisorReviewerId: users["aisha.diallo@mailinator.com"].Id,
                supFeedback: "Power distribution network verified against schematic. No anomalies. Approved for admin review.",
                supApproved: true,
                adminReviewerId: users["ibrahim.musa@mailinator.com"].Id,
                adminFeedback: "Excellent. Power systems milestone approved. Proceed to full integration.",
                adminApproved: true,
                mediaUrls: new List<string> { "https://res.cloudinary.com/demo/image/upload/power_bus_installed.jpg", "https://res.cloudinary.com/demo/image/upload/bms_readout.jpg" },
                mediaTypes: new List<string> { "image", "image" });

            // ── SS3: roberto submitted → Supervisor approved → AdminApproved (no contractor) ──
            await AddUpdate(tasks["SS3"].Id, users["roberto.silva@mailinator.com"].Id,
                "Procurement specification for PEM electrolysis cells submitted to three vendors. Awaiting quotes. Installation schedule drafted and attached.",
                UpdateStatus.AdminApproved, 10, DateTime.UtcNow.AddDays(-12),
                supervisorReviewerId: users["nadia.santos@mailinator.com"].Id,
                supFeedback: "Procurement scope correctly defined. Vendor list approved. Good progress.",
                supApproved: true,
                adminReviewerId: users["ibrahim.musa@mailinator.com"].Id,
                adminFeedback: "Approved. Ensure delivery lead times are tracked weekly.",
                adminApproved: true);

            // ── ST1: sara submitted → UnderContractorAdminReview (SteelBuild → olumide) ──
            await AddUpdate(tasks["ST1"].Id, users["sara.berg@mailinator.com"].Id,
                "East stand piling 100% complete – 240 piles installed and load-tested. West stand piling 70% complete – 168 of 240 piles done. North/South base slab design finalised.",
                UpdateStatus.UnderContractorAdminReview, 55, DateTime.UtcNow.AddDays(-1),
                mediaUrls: new List<string> { "https://res.cloudinary.com/demo/image/upload/pile_east_complete.jpg", "https://res.cloudinary.com/demo/image/upload/pile_west_progress.jpg" },
                mediaTypes: new List<string> { "image", "image" });

            // ── ST1: zara submitted (2nd update) → also UnderContractorAdminReview ──
            await AddUpdate(tasks["ST1"].Id, users["zara.okonkwo@mailinator.com"].Id,
                "West stand piling update: piles 169-200 completed today. Ground beam reinforcement cages being assembled in casting yard for next phase.",
                UpdateStatus.UnderContractorAdminReview, 58, DateTime.UtcNow.AddHours(-6),
                mediaUrls: new List<string> { "https://res.cloudinary.com/demo/image/upload/west_pile_detail.jpg" },
                mediaTypes: new List<string> { "image" });

            // ── ST3: yusuf submitted → UnderSupervisorReview (no contractor) ──
            await AddUpdate(tasks["ST3"].Id, users["yusuf.adamu@mailinator.com"].Id,
                "All four mast foundations excavated and poured. Mast 1 (NW corner) fully erected at 55 m. Mast 2 (NE corner) base section in place. Requesting supervisor approval to continue.",
                UpdateStatus.UnderSupervisorReview, 25, DateTime.UtcNow.AddDays(-2),
                mediaUrls: new List<string> { "https://res.cloudinary.com/demo/image/upload/mast_nw_erected.jpg", "https://res.cloudinary.com/demo/image/upload/mast_ne_base.jpg" },
                mediaTypes: new List<string> { "image", "image" });

            // ── ST4: roberto submitted → UnderContractorAdminReview (EcoGrass → mei.lin) ──
            await AddUpdate(tasks["ST4"].Id, users["roberto.silva@mailinator.com"].Id,
                "Drainage design reviewed and approved by structural team. Sub-base material delivered on site – 8,400 m³ recycled aggregate. Compaction programme scheduled to begin next week.",
                UpdateStatus.UnderContractorAdminReview, 5, DateTime.UtcNow.AddDays(-3),
                mediaUrls: new List<string> { "https://res.cloudinary.com/demo/image/upload/subbase_delivery.jpg" },
                mediaTypes: new List<string> { "image" });

            // ── ST5: kwame submitted → Supervisor approved → AdminApproved (no contractor) ──
            await AddUpdate(tasks["ST5"].Id, users["kwame.asante@mailinator.com"].Id,
                "All 24 VIP boxes shell-out complete. M&E first fix complete in all boxes. AV and climate control installation 70% done. Premium seating being installed in Club Level concourse.",
                UpdateStatus.AdminApproved, 75, DateTime.UtcNow.AddDays(-4),
                supervisorReviewerId: users["elena.novak@mailinator.com"].Id,
                supFeedback: "Quality of VIP fit-out exceeds specification. AV system demo passed. Approved for admin.",
                supApproved: true,
                adminReviewerId: users["pascal.dubois@mailinator.com"].Id,
                adminFeedback: "Premium hospitality area approved. Final snagging list to be resolved before handover.",
                adminApproved: true,
                mediaUrls: new List<string> { "https://res.cloudinary.com/demo/image/upload/vip_shell_done.jpg", "https://res.cloudinary.com/demo/image/upload/club_level_seating.jpg" },
                mediaTypes: new List<string> { "image", "image" });

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded task updates.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  TASK COMMENTS
        // ─────────────────────────────────────────────────────────────────────────
        private async Task SeedTaskCommentsAsync(
            Dictionary<string, ProjectTask> tasks,
            Dictionary<string, ProjectTask> subtasks,
            Dictionary<string, User> users,
            CancellationToken ct)
        {
            _logger.LogInformation("Seeding task comments...");
            var all = tasks.ToDictionary(kv => kv.Key, kv => kv.Value);
            foreach (var kv in subtasks) all[kv.Key] = kv.Value;

            var comments = new (string TaskKey, string Email, string Text)[]
            {
                // ── SS1 ────────────────────────────────────────────────────────
                ("SS1",   "elena.novak@mailinator.com",    "Combustion chamber geometry finalised after 3 CFD iterations. Thrust-to-weight ratio now exceeds target by 6%. Good progress."),
                ("SS1",   "ibrahim.musa@mailinator.com",   "Please ensure all test stand instrumentation is calibrated before the static fire. Certification team will be on site."),
                ("SS1",   "kwame.asante@mailinator.com",   "Machining coolant temperature issue on Nozzle Rev 4 resolved. CNC programme updated. No schedule impact."),
                ("SS1",   "zara.okonkwo@mailinator.com",   "Outstanding progress on the propulsion module. Static fire will be a major milestone for the programme."),
                // ── SS2 ────────────────────────────────────────────────────────
                ("SS2",   "aisha.diallo@mailinator.com",   "Navigation core module peer-reviewed by external consultant and passed without comment. Excellent work Yusuf."),
                ("SS2",   "yusuf.adamu@mailinator.com",    "HITL simulation running 40x real-time with less than 0.8 ms latency on the guidance loop. Will optimise further."),
                ("SS2",   "sophie.laurent@mailinator.com", "Sensor fusion algorithm now handles GPS outages gracefully. Switched to pure-inertial mode is seamless."),
                ("SS2",   "ibrahim.musa@mailinator.com",   "Reminder: all avionics software must pass DO-178C Level A formal review before integration freeze."),
                // ── SS3 ────────────────────────────────────────────────────────
                ("SS3",   "nadia.santos@mailinator.com",   "Life support bay structural mounting points confirmed with Hull team. Procurement can proceed on schedule."),
                ("SS3",   "roberto.silva@mailinator.com",  "Vendor 2 submitted a preliminary quote 15% under budget. Full quote expected by end of month."),
                ("SS3",   "chinwe.obiora@mailinator.com",  "Thermal analysis confirms the scrubber mount location is acceptable for heat load. No redesign needed."),
                // ── SS4 ────────────────────────────────────────────────────────
                ("SS4",   "hassan.ali@mailinator.com",     "Forward fuselage panels passed NDT inspection with zero defects. Thermal protection coating adhesion test results will follow."),
                ("SS4",   "mei.lin@mailinator.com",        "Titanium billet delivery confirmed for next week. Sufficient stock for aft section panels. No supply chain risk."),
                ("SS4",   "ibrahim.musa@mailinator.com",   "Well done. Hull timeline is critical path for integration. Keep up this pace."),
                // ── SS5 ────────────────────────────────────────────────────────
                ("SS5",   "aisha.diallo@mailinator.com",   "Power distribution harness routing approved. Cable shielding spec upgraded to MIL-STD-1553 to reduce EMI."),
                ("SS5",   "emeka.eze@mailinator.com",      "Battery management firmware update v2.1 deployed. Discharge curve now matches theoretical within 1.2%. Approved."),
                ("SS5",   "zara.okonkwo@mailinator.com",   "This milestone is approved. SS5 is a programme critical item. Well done to the avionics and AeroSystems team."),
                // ── ST1 ────────────────────────────────────────────────────────
                ("ST1",   "kofi.mensah@mailinator.com",    "East stand piling pile integrity tests all passed. Confirm with structural engineer before releasing the load test certificates."),
                ("ST1",   "adaeze.onuoha@mailinator.com",  "Excellent foundation progress. The contractor is ahead of the piling programme by 4 days. Keep it up."),
                ("ST1",   "sara.berg@mailinator.com",      "Ground investigation confirmed no unexpected obstructions in west stand zone. Piling can continue at planned rate."),
                ("ST1",   "pascal.dubois@mailinator.com",  "Reminder to all teams: the critical path for the stadium is the foundation. Any delay here cascades across all other packages."),
                ("ST1",   "zara.okonkwo@mailinator.com",   "Pile cap formwork for East stand ready for pour next Monday. Concrete mix design approved by structural engineer."),
                // ── ST2 ────────────────────────────────────────────────────────
                ("ST2",   "elena.novak@mailinator.com",    "Seating frame fabrication drawings issued to steelwork fabricator. Shop drawings review expected within 3 weeks."),
                ("ST2",   "kwame.asante@mailinator.com",   "I have reviewed the riser calculations for upper tier. The 34° rake angle meets safety and sightline requirements."),
                ("ST2",   "pascal.dubois@mailinator.com",  "Upper tier seating is a key spectator experience element. Ensure all accessible seating positions comply with disability standards."),
                // ── ST3 ────────────────────────────────────────────────────────
                ("ST3",   "aisha.diallo@mailinator.com",   "Mast NW completed ahead of schedule. Independent structural check passed. Excellent erection team performance."),
                ("ST3",   "yusuf.adamu@mailinator.com",    "Mast NE base fixing bolts torqued to spec. Erection crew on stand-by pending favourable wind forecast tomorrow."),
                ("ST3",   "adaeze.onuoha@mailinator.com",  "The LED floodlight specification has been upgraded to 3,000 lux average for broadcast compliance. Updated drawings issued."),
                ("ST3",   "emeka.eze@mailinator.com",      "Switchgear delivery confirmed for week 28. Cable drum staging area allocated on site. No delays anticipated."),
                // ── ST4 ────────────────────────────────────────────────────────
                ("ST4",   "nadia.santos@mailinator.com",   "Irrigation design reviewed by EcoGrass agronomist. Zone layout approved. 14 irrigation zones covering the full 68x105 m pitch."),
                ("ST4",   "roberto.silva@mailinator.com",  "Sub-base aggregate specification upgraded to recycled concrete aggregate to meet the project sustainability target."),
                ("ST4",   "mei.lin@mailinator.com",        "Grass hybrid variety selected: Desso GrassMaster 80% natural / 20% synthetic fibre. Lead time 16 weeks from order."),
                ("ST4",   "adaeze.onuoha@mailinator.com",  "Pitch quality is a flagship element of this project. All decisions on grass and irrigation must be approved by the project owner."),
                // ── ST5 ────────────────────────────────────────────────────────
                ("ST5",   "kwame.asante@mailinator.com",   "VIP box B3 and B4 AV system snagging items resolved. All 24 boxes now at practical completion. Final inspection booked."),
                ("ST5",   "felix.wagner@mailinator.com",   "Climate control zoning tested across all boxes. Each zone holds temperature within ±0.5°C. Excellent installation quality."),
                ("ST5",   "pascal.dubois@mailinator.com",  "VIP hospitality package approved by operator. Catering infrastructure signed off. Final handover scheduled for next week."),
                ("ST5",   "adaeze.onuoha@mailinator.com",  "The VIP suites are exceptional. This will be a selling point for the stadium. Well done to the seating team."),
                // ── Subtask SS1_B ──────────────────────────────────────────────
                ("SS1_B", "priya.sharma@mailinator.com",   "Inconel 718 billet arrives Thursday. CNC programme validated on the simulator – no collisions. Machining starts Friday."),
                ("SS1_B", "elena.novak@mailinator.com",    "Ensure the nozzle surface finish Ra ≤ 0.8 µm on the inner throat profile. This is critical for combustion stability."),
                // ── Subtask ST1_B ──────────────────────────────────────────────
                ("ST1_B", "zara.okonkwo@mailinator.com",   "West stand pile 185 showed 5 mm deviation on verticality. Re-tested – within 1:200 tolerance. No remedial action needed."),
                ("ST1_B", "kofi.mensah@mailinator.com",    "Good catch on pile 185. All verticality readings should be reported daily in the piling record sheet going forward."),
                // ── Subtask ST5_B ──────────────────────────────────────────────
                ("ST5_B", "felix.wagner@mailinator.com",   "4K displays calibrated to D65 colour standard. Audio system balanced at 85 dB SPL at the rear of each box. Quality is excellent."),
                ("ST5_B", "elena.novak@mailinator.com",    "AV system commissioning certificates required for project handover. Please ensure all certs are uploaded to the document management system."),
            };

            foreach (var (taskKey, email, text) in comments)
            {
                if (!all.TryGetValue(taskKey, out var task)) continue;
                var user = await _unitOfWork.Users.GetByEmailAsync(email, ct);
                if (user == null) continue;
                if (await _unitOfWork.TaskComments.AnyAsync(tc => tc.TaskId == task.Id && tc.UserId == user.Id && tc.Comment == text, ct)) continue;
                await _unitOfWork.TaskComments.AddAsync(new TaskComment
                {
                    Id = Guid.NewGuid(), TaskId = task.Id, UserId = user.Id,
                    Comment = text, CreatedAt = DateTime.UtcNow.AddHours(-new Random().Next(1, 240)),
                }, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded task comments.");
        }
    }
}
