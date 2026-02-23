using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Hangfire;
using Hangfire.PostgreSql;
using ApexBuild.Infrastructure.Persistence;
using ApexBuild.Infrastructure.BackgroundJobs;
using Hangfire.Dashboard;
using ApexBuild.Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

using ApexBuild.Application;
using ApexBuild.Infrastructure;
using ApexBuild.Infrastructure.Configurations;


var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/apexbuild-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();


// Add services to the container.

builder.Host.UseSerilog();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ApexBuild API",
        Version = "v1",
        Description = "Construction Project Management and Tracking Platform API",
        Contact = new OpenApiContact
        {
            Name = "ApexBuild Support",
            Email = "support@apexbuild.com"
        }
    });

    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Bind configuration sections to strongly typed classes
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.Configure<SendGridSettings>(builder.Configuration.GetSection("SendGrid"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("App"));

var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(jwtSecret != null ? Encoding.UTF8.GetBytes(jwtSecret) : Array.Empty<byte>()),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
//-StartupProject ApexBuild.Api -Project ApexBuild.Infrastructure
// Configure DbContext (single registration)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("ApexBuild.Infrastructure");
        })
        .UseSnakeCaseNamingConvention();
});

// Configure Hangfire
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("HangfireConnection"));
    }));

builder.Services.AddHangfireServer();

// Configure CORS with better security
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
        ?? new[] { builder.Configuration["App:FrontendUrl"] ?? "http://localhost:5173" };

    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    // Development fallback
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    }
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("hangfire", () =>
    {
        // Basic Hangfire health check
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Hangfire is operational");
    }, tags: new[] { "hangfire" });

builder.Services.AddApplicationServices();

builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddHttpContextAccessor();

// Add Response Caching
builder.Services.AddResponseCaching();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.

// CORS must be FIRST — before exception handler — so CORS headers are present
// even on 500 responses. Otherwise the browser reports a CORS error instead of the real 500.
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("CorsPolicy");
}

// Forward headers from Render's reverse proxy (X-Forwarded-Proto, X-Forwarded-For)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Global Exception Handler
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Request Logging
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Swagger available in all environments so the deployed API can be tested
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ApexBuild API V1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
    c.EnableFilter();
});

app.UseSerilogRequestLogging();

// HTTPS redirect is handled by Render's reverse proxy — skip in production to avoid loops
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Response Caching
app.UseResponseCaching();

// Health Checks - Database check
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        // Add database check
        var dbHealthy = true;
        string? dbError = null;
        try
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbHealthy = await dbContext.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            dbHealthy = false;
            dbError = ex.Message;
        }

        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description ?? string.Empty,
                duration = e.Value.Duration.TotalMilliseconds
            }).Concat(new[]
            {
                new
                {
                    name = "database",
                    status = dbHealthy ? "Healthy" : "Unhealthy",
                    description = dbError ?? "Database is accessible",
                    duration = 0.0
                }
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.UseAuthentication();
app.UseAuthorization();


app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

BackgroundJobScheduler.ScheduleRecurringJobs();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await context.Database.MigrateAsync();
        Log.Information("Database migrated successfully");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An error occurred while migrating the database");
        throw;
    }
}


Log.Information("ApexBuild API starting...");

app.Run();


public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // In development, allow all
        if (httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return true;
        }

        // In production, require authentication and admin role
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        // Check if user has Platform Admin role
        return httpContext.User.HasClaim(ClaimTypes.Role, "PlatformAdmin") ||
               httpContext.User.IsInRole("PlatformAdmin");
    }
}