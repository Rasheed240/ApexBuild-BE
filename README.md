# ApexBuild API

A robust RESTful API backend for **ApexBuild** - a Construction Project Management and Tracking Platform built with .NET 9 and Clean Architecture.

![Dashboard](assets/dashboard.png)

![Settings](assets/settings.png)

<!-- Demo Video -->
https://res.cloudinary.com/dok7bqllf/video/upload/v1770725397/Apexdemo_yvwhbm.mp4

## Features

- **Authentication & Authorization** - JWT-based auth with refresh tokens, role-based access control, email confirmation, and password reset
- **Two-Factor Authentication** - TOTP-based 2FA setup, verification, and management
- **Organization Management** - Multi-tenant support with organizations, departments, member roles, and invitations
- **Project Management** - Full CRUD for projects with progress tracking, team assignment, and status workflows
- **Task Management** - Task creation, assignment (multiple assignees), daily update submissions, supervisor/admin review and approval workflows, comments
- **Subscription & Payments** - Stripe integration for subscriptions, payment processing, invoices, proration, and webhook handling
- **Notifications** - In-app notification system with read/unread tracking and email notifications via SendGrid
- **Dashboard & Analytics** - Real-time project metrics, activity feeds, task completion stats, and team performance data
- **Media Uploads** - Cloudinary integration for image/file uploads
- **Audit Logging** - Comprehensive audit trail for all system actions
- **Background Jobs** - Hangfire-powered scheduled jobs for reminders, deadline alerts, birthday notifications, and cleanup tasks
- **Licensing** - Organization license management and enforcement

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Framework** | .NET 9 / ASP.NET Core |
| **Architecture** | Clean Architecture (Domain, Application, Infrastructure, API) |
| **Database** | PostgreSQL with Entity Framework Core 9 |
| **Authentication** | JWT Bearer tokens + Refresh tokens |
| **Payments** | Stripe.net |
| **Email** | SendGrid |
| **Media Storage** | Cloudinary |
| **Background Jobs** | Hangfire with PostgreSQL storage |
| **Caching** | Redis (StackExchange.Redis) |
| **Logging** | Serilog (Console, File, Seq sinks) |
| **API Docs** | Swagger / Swashbuckle |
| **Validation** | FluentValidation via MediatR pipeline |
| **Mapping** | AutoMapper |
| **2FA** | OTP.NET (TOTP) |
| **Hashing** | BCrypt.Net |
| **Containerization** | Docker + Docker Compose |
| **Deployment** | Render |

## Project Structure

```
ApexBuild-BE/
├── ApexBuild.Api/              # API layer - Controllers, Middleware, Program.cs
│   ├── Controllers/            # 17 API controllers
│   └── Middleware/              # Exception handling, request logging
├── ApexBuild.Application/      # Application layer - CQRS commands/queries
│   ├── Common/                 # Interfaces, exceptions, behaviours
│   └── Features/               # Authentication, Organizations, Projects, Tasks, etc.
├── ApexBuild.Contracts/        # Shared DTOs, request/response models
├── ApexBuild.Domain/           # Domain layer - Entities, enums, base classes
│   ├── Entities/               # 21 domain entities
│   └── Enums/                  # Status types, role types, etc.
├── ApexBuild.Infrastructure/   # Infrastructure layer - EF Core, services, repositories
│   ├── BackgroundJobs/         # Hangfire scheduled jobs
│   ├── Configurations/         # Settings classes (JWT, Stripe, Email, etc.)
│   ├── Data/                   # Database seeder
│   ├── Migrations/             # EF Core migrations
│   ├── Persistence/            # DbContext and entity configurations
│   ├── Repositories/           # Repository implementations
│   └── Services/               # Service implementations
├── ApexBuild.Tests/            # Unit and integration tests
├── Dockerfile                  # Container configuration
├── docker-compose.yml          # Multi-service orchestration
└── render.yaml                 # Render deployment config
```

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/) 15+
- [Redis](https://redis.io/download) (optional, for caching)

### Setup

1. Clone the repository:
```bash
git clone <repo-url>
cd ApexBuild-BE
```

2. Create `appsettings.Development.json` in `ApexBuild.Api/`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=apexbuild;Username=postgres;Password=your_password"
  },
  "JwtSettings": {
    "Secret": "your-secret-key-at-least-32-characters-long",
    "Issuer": "ApexBuild",
    "Audience": "ApexBuild",
    "ExpiryMinutes": 60
  },
  "StripeSettings": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_..."
  },
  "CloudinarySettings": {
    "CloudName": "your-cloud-name",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret"
  },
  "SendGridSettings": {
    "ApiKey": "your-sendgrid-key"
  }
}
```

3. Run database migrations:
```bash
cd ApexBuild.Api
dotnet ef database update --project ../ApexBuild.Infrastructure
```

4. Run the application:
```bash
dotnet run --project ApexBuild.Api
```

5. Open Swagger UI at `https://localhost:44361/swagger`

### Running with Docker

```bash
docker-compose up --build
```

## API Endpoints

| Area | Endpoints |
|------|----------|
| **Auth** | `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh-token`, `POST /api/auth/forgot-password`, `POST /api/auth/reset-password` |
| **2FA** | `POST /api/auth/2fa/enable`, `POST /api/auth/2fa/verify-setup`, `POST /api/auth/2fa/verify`, `POST /api/auth/2fa/disable` |
| **Organizations** | CRUD + member management, departments |
| **Projects** | CRUD + progress tracking, team assignment |
| **Tasks** | CRUD + assignments, updates, reviews, comments |
| **Subscriptions** | Plans, checkout, billing management |
| **Payments** | Transaction history, invoices |
| **Notifications** | List, mark read, delete |
| **Dashboard** | Stats, metrics, recent activity |
| **Media** | Upload, delete |
| **Audit Logs** | Query audit trail |

## Running Tests

```bash
dotnet test
```

## License

This project is proprietary software.
