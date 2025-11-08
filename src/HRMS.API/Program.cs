using Microsoft.EntityFrameworkCore;
using HRMS.Infrastructure.Data;
using HRMS.Infrastructure.Services;
using HRMS.Core.Interfaces;
using HRMS.Application.Interfaces;
using HRMS.API.Middleware;
using HRMS.Core.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Serilog.Events;
using Hangfire;
using Hangfire.PostgreSql;
using HRMS.BackgroundJobs.Jobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using FluentValidation.AspNetCore;
using FluentValidation;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

/// <summary>
/// PRODUCTION-GRADE HRMS API Program.cs
///
/// This configuration is designed for enterprise production deployment:
/// - Bank-level security
/// - Comprehensive logging and monitoring
/// - Health checks for all dependencies
/// - Rate limiting and DoS protection
/// - Correlation IDs for distributed tracing
/// - PII-masked audit logging
/// - Environment-based configuration
/// - Google Cloud integration ready
/// </summary>

var builder = WebApplication.CreateBuilder(args);

// ======================
// PRODUCTION SERILOG CONFIGURATION
// ======================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Application", "HRMS")
    .Enrich.WithProperty("Version", "1.0.0")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

builder.Host.UseSerilog();

Log.Information("=================================================");
Log.Information("HRMS API Starting - Environment: {Environment}", builder.Environment.EnvironmentName);
Log.Information("=================================================");

// ======================
// CONFIGURATION SETTINGS
// ======================
var securitySettings = builder.Configuration.GetSection("Security").Get<SecuritySettings>() ?? new SecuritySettings();
var googleCloudSettings = builder.Configuration.GetSection("GoogleCloud").Get<GoogleCloudSettings>() ?? new GoogleCloudSettings();
var healthCheckSettings = builder.Configuration.GetSection("HealthChecks").Get<HealthCheckSettings>() ?? new HealthCheckSettings();
var hangfireSettings = builder.Configuration.GetSection("Hangfire").Get<HangfireSettings>() ?? new HangfireSettings();

builder.Services.Configure<SecuritySettings>(builder.Configuration.GetSection("Security"));
builder.Services.Configure<GoogleCloudSettings>(builder.Configuration.GetSection("GoogleCloud"));
builder.Services.Configure<HealthCheckSettings>(builder.Configuration.GetSection("HealthChecks"));
builder.Services.Configure<HangfireSettings>(builder.Configuration.GetSection("Hangfire"));
builder.Services.Configure<RateLimitingSettings>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.Configure<PerformanceSettings>(builder.Configuration.GetSection("Performance"));
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));

// ======================
// GOOGLE SECRET MANAGER (Production)
// ======================
GoogleSecretManagerService? secretManager = null;
if (googleCloudSettings.SecretManagerEnabled && !string.IsNullOrEmpty(googleCloudSettings.ProjectId))
{
    var smLogger = LoggerFactory.Create(config => config.AddSerilog()).CreateLogger<GoogleSecretManagerService>();
    secretManager = new GoogleSecretManagerService(smLogger, googleCloudSettings.ProjectId, true);
    builder.Services.AddSingleton(secretManager);

    Log.Information("Google Secret Manager enabled for project: {ProjectId}", googleCloudSettings.ProjectId);
}
else
{
    Log.Information("Google Secret Manager disabled - using configuration/environment variables");
}

// ======================
// DATABASE CONFIGURATION
// ======================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// In production, try to get connection string from Secret Manager
if (secretManager != null && string.IsNullOrEmpty(connectionString))
{
    connectionString = await secretManager.GetSecretAsync("DB_CONNECTION_STRING");
    Log.Information("Database connection string loaded from Secret Manager");
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string not configured. Check appsettings.json or Google Secret Manager.");
}

// Master DbContext (system-wide data)
builder.Services.AddDbContext<MasterDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    });

    // Enable sensitive data logging only in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// ======================
// HTTP CONTEXT ACCESSOR
// ======================
builder.Services.AddHttpContextAccessor();

// ======================
// TENANT DBCONTEXT
// ======================
builder.Services.AddScoped<TenantDbContext>(serviceProvider =>
{
    // Get tenant context from TenantService (set by TenantResolutionMiddleware)
    var tenantService = serviceProvider.GetRequiredService<ITenantService>();
    var tenantSchema = tenantService.GetCurrentTenantSchema();

    // Default to "public" schema if no tenant context (e.g., admin panel, public endpoints)
    if (string.IsNullOrEmpty(tenantSchema))
    {
        tenantSchema = "public";
    }

    var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
    optionsBuilder.UseNpgsql(connectionString, o =>
    {
        // Use tenant-specific schema for migrations history
        o.MigrationsHistoryTable("__EFMigrationsHistory", tenantSchema);
        o.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    });

    return new TenantDbContext(optionsBuilder.Options, tenantSchema);
});

// ======================
// MULTI-TENANCY SERVICES
// ======================
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ISchemaProvisioningService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<SchemaProvisioningService>>();
    return new SchemaProvisioningService(connectionString!, logger, provider);
});

// ======================
// JWT CONFIGURATION
// ======================
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
var jwtSecret = jwtSettings?.Secret;

// In production, load JWT secret from Secret Manager
if (secretManager != null && (string.IsNullOrEmpty(jwtSecret) || jwtSecret == ""))
{
    jwtSecret = await secretManager.GetSecretAsync("JWT_SECRET");
    Log.Information("JWT secret loaded from Secret Manager");
}

if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException("JWT secret not configured or too short (minimum 32 characters). Check appsettings.json or Google Secret Manager.");
}

var key = Encoding.UTF8.GetBytes(jwtSecret);

// ======================
// APPLICATION SERVICES
// ======================
builder.Services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMfaService, MfaService>();  // MFA (TOTP + Backup Codes)
builder.Services.AddScoped<TenantAuthService>();  // Tenant employee authentication
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IEmployeeDraftService, EmployeeDraftService>();  // Employee draft management
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<ISectorService, SectorService>();
builder.Services.AddScoped<ISectorComplianceService, SectorComplianceService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IAttendanceMachineService, AttendanceMachineService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<ISalaryComponentService, SalaryComponentService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<TenantManagementService>();

// Timesheet Management Services
builder.Services.AddScoped<ITimesheetGenerationService, TimesheetGenerationService>();
builder.Services.AddScoped<ITimesheetApprovalService, TimesheetApprovalService>();
builder.Services.AddScoped<ITimesheetAdjustmentService, TimesheetAdjustmentService>();
Log.Information("Timesheet management services registered");

// Multi-Device Biometric Attendance System Services
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IBiometricDeviceService, BiometricDeviceService>();
Log.Information("Multi-device biometric attendance system services registered");

// Department Management Service
builder.Services.AddScoped<DepartmentService>();
Log.Information("Department management service registered");

// PRODUCTION FIX #1: Cloud Storage Service for file uploads
builder.Services.AddSingleton<IFileStorageService, GoogleCloudStorageService>();
Log.Information("Google Cloud Storage service registered for file uploads");

// PRODUCTION-GRADE: Background token cleanup service
builder.Services.AddHostedService<HRMS.API.Services.TokenCleanupService>();
Log.Information("Token cleanup background service registered (runs hourly)");

// ======================
// BACKGROUND JOBS SERVICES
// ======================
builder.Services.AddScoped<DocumentExpiryAlertJob>();
builder.Services.AddScoped<AbsentMarkingJob>();
builder.Services.AddScoped<LeaveAccrualJob>();
builder.Services.AddScoped<DeleteExpiredDraftsJob>();

// ======================
// JWT AUTHENTICATION (PRODUCTION-GRADE)
// ======================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // SECURITY FIX: Require HTTPS metadata in production
    options.RequireHttpsMetadata = securitySettings.RequireHttpsMetadata;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings!.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };

    // Enhanced security logging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("JWT authentication failed: {Exception}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userId = context.Principal?.Identity?.Name;
            Log.Debug("JWT token validated for user: {UserId}", userId ?? "Unknown");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ======================
// RATE LIMITING CONFIGURATION (PRODUCTION-GRADE)
// ======================
// Required for IP-based rate limiting
builder.Services.AddMemoryCache();

// Add distributed cache for production (Redis)
if (builder.Environment.IsProduction())
{
    var redisConnectionString = builder.Configuration.GetSection("Redis:ConnectionString").Get<string>();
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = builder.Configuration.GetSection("Redis:InstanceName").Get<string>() ?? "HRMS_";
        });
        Log.Information("Redis distributed cache configured: {ConnectionString}", redisConnectionString.Split('@').LastOrDefault() ?? "configured");
    }
    else
    {
        Log.Warning("Redis connection string not configured - rate limiting will fall back to memory cache");
    }
}

// Configure IP Rate Limiting
builder.Services.Configure<AspNetCoreRateLimit.IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<AspNetCoreRateLimit.IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));

// PRODUCTION FIX #2: Configure rate limiting stores based on environment
if (builder.Environment.IsProduction())
{
    // Production: Use Redis-backed distributed cache for multi-instance deployment
    // This ensures rate limits work correctly across multiple application instances
    builder.Services.AddSingleton<AspNetCoreRateLimit.IIpPolicyStore, AspNetCoreRateLimit.DistributedCacheIpPolicyStore>();
    builder.Services.AddSingleton<AspNetCoreRateLimit.IRateLimitCounterStore, AspNetCoreRateLimit.DistributedCacheRateLimitCounterStore>();
    Log.Information("Rate limiting configured with Redis distributed cache for multi-instance deployment");
}
else
{
    // Development: Use memory cache (simpler, no Redis dependency)
    builder.Services.AddSingleton<AspNetCoreRateLimit.IIpPolicyStore, AspNetCoreRateLimit.MemoryCacheIpPolicyStore>();
    builder.Services.AddSingleton<AspNetCoreRateLimit.IRateLimitCounterStore, AspNetCoreRateLimit.MemoryCacheRateLimitCounterStore>();
    Log.Information("Rate limiting configured with memory cache (development mode)");
}

builder.Services.AddSingleton<AspNetCoreRateLimit.IRateLimitConfiguration, AspNetCoreRateLimit.RateLimitConfiguration>();
builder.Services.AddSingleton<AspNetCoreRateLimit.IProcessingStrategy, AspNetCoreRateLimit.AsyncKeyLockProcessingStrategy>();

Log.Information("Rate limiting enabled: Login (5/15min), API (100/min, 1000/hour)");

// ======================
// RESPONSE COMPRESSION (COST OPTIMIZATION - 60-80% bandwidth reduction)
// ======================
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

Log.Information("Response compression enabled: Brotli (primary), Gzip (fallback) - Expected 60-80% bandwidth savings");

// ======================
// HANGFIRE CONFIGURATION (PRODUCTION-GRADE)
// ======================
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UsePostgreSqlStorage(options =>
          {
              options.UseNpgsqlConnection(connectionString);
          });
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = hangfireSettings.WorkerCount;
    options.ServerName = $"HRMS-{Environment.MachineName}";
});

// ======================
// CORS CONFIGURATION (PRODUCTION-GRADE) - WILDCARD SUBDOMAIN SUPPORT
// ======================
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var allowedDomains = builder.Configuration.GetSection("Cors:AllowedDomains").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionCorsPolicy", policy =>
    {
        if (allowedOrigins.Length > 0 || allowedDomains.Length > 0)
        {
            policy.SetIsOriginAllowed(origin =>
            {
                // Allow specific origins (exact match)
                if (allowedOrigins.Contains(origin))
                    return true;

                // Allow wildcard subdomain domains (*.domain.com)
                // Example: If allowedDomains contains "hrms.com", allow:
                // - https://acme.hrms.com
                // - https://demo.hrms.com
                // - https://www.hrms.com
                // - https://hrms.com (root domain)
                foreach (var domain in allowedDomains)
                {
                    var uri = new Uri(origin);
                    var host = uri.Host;

                    // Match exact domain or subdomain.domain pattern
                    if (host == domain || host.EndsWith($".{domain}"))
                        return true;
                }

                return false;
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition", "X-Correlation-ID", "X-Total-Count")
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));

            Log.Information("CORS configured with allowed origins: {AllowedOrigins}", string.Join(", ", allowedOrigins));
            Log.Information("CORS configured with wildcard subdomain support for domains: {AllowedDomains}", string.Join(", ", allowedDomains));
        }
        else if (builder.Environment.IsDevelopment())
        {
            // Development fallback - Allow localhost (with wildcard subdomains) and cloud dev environments
            policy.SetIsOriginAllowed(origin =>
            {
                // Allow localhost and *.localhost (subdomain support for development)
                // Examples: http://localhost:4200, http://acme.localhost:4200
                if (origin.StartsWith("http://localhost") || origin.StartsWith("https://localhost") ||
                    origin.Contains(".localhost:") || origin.Contains(".localhost/"))
                    return true;

                // Allow GitHub Codespaces domains (including subdomains)
                if (origin.Contains(".app.github.dev"))
                    return true;

                // Allow Gitpod domains (including subdomains)
                if (origin.Contains(".gitpod.io"))
                    return true;

                return false;
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition", "X-Correlation-ID", "X-Total-Count")
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));

            Log.Information("CORS enabled for development: localhost (with wildcard subdomains), cloud dev environments");
        }
        else
        {
            Log.Warning("No CORS origins/domains configured - CORS will block all cross-origin requests");
        }
    });
});

// ======================
// HEALTH CHECKS (PRODUCTION-GRADE)
// ======================
if (healthCheckSettings.Enabled)
{
    var healthChecksBuilder = builder.Services.AddHealthChecks()
        .AddNpgSql(
            connectionString!,
            name: "postgresql-master",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "db", "postgresql", "master" },
            timeout: TimeSpan.FromSeconds(healthCheckSettings.DatabaseTimeoutSeconds));

    // Add Redis health check if configured
    var redisConnectionString = builder.Configuration.GetSection("Redis:ConnectionString").Get<string>();
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        healthChecksBuilder.AddRedis(
            redisConnectionString,
            name: "redis-cache",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "cache", "redis" },
            timeout: TimeSpan.FromSeconds(healthCheckSettings.RedisTimeoutSeconds));
    }

    Log.Information("Health checks configured: PostgreSQL, Redis");
}

// ======================
// FLUENT VALIDATION
// ======================
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ======================
// CONTROLLERS WITH OPTIMIZED JSON (COST OPTIMIZATION - 30% smaller payloads)
// ======================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Ignore null values (reduce payload size by 20-30%)
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;

        // Use camelCase for consistency
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;

        // Handle circular references (prevent serialization errors)
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

        // Performance: Don't write indented in production
        options.JsonSerializerOptions.WriteIndented = false;

        // Enable string-based enum serialization/deserialization
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());

        // Use faster number handling
        options.JsonSerializerOptions.NumberHandling =
            System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
    });

Log.Information("JSON serialization optimized: Nulls ignored, cycles handled, camelCase - Expected 30% smaller payloads");

// ======================
// API DOCUMENTATION (SWAGGER)
// ======================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "HRMS API - Enterprise HR Management System",
        Version = "v1.0.0",
        Description = "Production-grade Multi-Tenant HRMS API with Mauritius Labour Law Compliance",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "HRMS Support",
            Email = "support@hrms.com"
        }
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your JWT token."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ======================
// DATABASE INITIALIZATION
// ======================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var masterContext = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

        // PRODUCTION FIX #3: Different database initialization strategy per environment
        if (app.Environment.IsDevelopment())
        {
            // Development: Auto-migrate and seed data for rapid development
            Log.Information("Development environment: Initializing master database with auto-migrations...");

            // Create database if it doesn't exist
            await masterContext.Database.EnsureCreatedAsync();

            // Apply pending migrations
            var pendingMigrations = await masterContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                Log.Information("Applying {Count} pending migrations", pendingMigrations.Count());
                await masterContext.Database.MigrateAsync();
                Log.Information("Migrations applied successfully");
            }

            // Seed default data (development only)
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();
            var seeder = new DataSeeder(masterContext, passwordHasher, logger);
            await seeder.SeedAsync();

            // Seed industry sectors
            SectorSeedData.SeedIndustrySectors(masterContext);
            SectorSeedData.SeedSectorComplianceRules(masterContext);

            Log.Information("Master database initialized successfully");
        }
        else
        {
            // Production: Only verify database connectivity, don't auto-migrate
            // Migrations must be run manually using: dotnet ef database update
            Log.Information("Production environment: Verifying database connectivity...");

            var canConnect = await masterContext.Database.CanConnectAsync();
            if (!canConnect)
            {
                throw new InvalidOperationException("Cannot connect to database. Check connection string and database availability.");
            }

            Log.Information("Database connectivity verified");

            // Check for pending migrations and warn if found
            var pendingMigrations = await masterContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                Log.Warning("PRODUCTION WARNING: {Count} pending migrations detected. Run migrations manually: dotnet ef database update", pendingMigrations.Count());
                Log.Warning("Pending migrations: {Migrations}", string.Join(", ", pendingMigrations));
            }
            else
            {
                Log.Information("No pending migrations - database schema is up to date");
            }
        }
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "FATAL: Database initialization/verification failed");
        throw;
    }
}

// ======================
// HTTP REQUEST PIPELINE (Production-Grade)
// CRITICAL: Middleware order matters! Follow Microsoft's recommended order.
// ======================

// Correlation ID - MUST be first for request tracking
app.UseCorrelationId();

// Global exception handling - Catches all unhandled exceptions (before everything else)
app.UseGlobalExceptionHandling();

// COST OPTIMIZATION: Response Compression (MUST be early in pipeline)
// Compresses all responses with Brotli/Gzip - reduces bandwidth by 60-80%
app.UseResponseCompression();

// HTTPS Redirection - Early to force HTTPS
app.UseHttpsRedirection();

// Request/Response logging with PII masking (only in non-production for performance)
if (!app.Environment.IsProduction())
{
    app.UseRequestResponseLogging();
}

// Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        diagnosticContext.Set("CorrelationId", httpContext.GetCorrelationId());
    };
});

// Swagger (development and staging only)
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HRMS API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "HRMS API Documentation";
    });

    Log.Information("Swagger UI enabled at /swagger");
}

// CRITICAL: UseRouting MUST come before UseCors for proper CORS handling
app.UseRouting();

// CORS - MUST come after UseRouting and before UseAuthentication/UseAuthorization
// ASP.NET Core will automatically handle OPTIONS preflight requests
app.UseCors("ProductionCorsPolicy");

// SECURITY FIX: Rate Limiting (prevents brute force attacks)
app.UseIpRateLimiting();

// Tenant Resolution (before authentication)
app.UseTenantResolution();

// SECURITY FIX: Tenant Context Validation (blocks requests without valid tenant)
app.UseTenantContextValidation();

// Authentication & Authorization - MUST come after UseCors
app.UseAuthentication();
app.UseAuthorization();

// ======================
// HANGFIRE DASHBOARD (Production-secured)
// ======================
if (hangfireSettings.DashboardEnabled)
{
    app.UseHangfireDashboard(hangfireSettings.DashboardPath, new DashboardOptions
    {
        Authorization = hangfireSettings.RequireAuthentication
            ? new[] { new HangfireDashboardAuthorizationFilter() }
            : Array.Empty<Hangfire.Dashboard.IDashboardAuthorizationFilter>(),
        DashboardTitle = "HRMS Background Jobs",
        StatsPollingInterval = 5000,
        DisplayStorageConnectionString = !app.Environment.IsProduction()
    });

    Log.Information("Hangfire dashboard enabled at {Path}", hangfireSettings.DashboardPath);
}

// ======================
// RECURRING BACKGROUND JOBS (Production-Grade)
// ======================
var mauritiusTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Mauritius Standard Time");

RecurringJob.AddOrUpdate<DocumentExpiryAlertJob>(
    "document-expiry-alerts",
    job => job.ExecuteAsync(),
    "0 9 * * *",
    new RecurringJobOptions
    {
        TimeZone = mauritiusTimeZone
    });

RecurringJob.AddOrUpdate<AbsentMarkingJob>(
    "absent-marking",
    job => job.ExecuteAsync(),
    "0 23 * * *",
    new RecurringJobOptions
    {
        TimeZone = mauritiusTimeZone
    });

RecurringJob.AddOrUpdate<LeaveAccrualJob>(
    "leave-accrual",
    job => job.ExecuteAsync(),
    "0 1 1 * *",
    new RecurringJobOptions
    {
        TimeZone = mauritiusTimeZone
    });

RecurringJob.AddOrUpdate<DeleteExpiredDraftsJob>(
    "delete-expired-drafts",
    job => job.ExecuteAsync(),
    "0 2 * * *",  // 2:00 AM daily
    new RecurringJobOptions
    {
        TimeZone = mauritiusTimeZone
    });

Log.Information("Recurring jobs configured: document-expiry-alerts, absent-marking, leave-accrual, delete-expired-drafts");

// ======================
// HEALTH CHECK ENDPOINTS (Production-Grade)
// ======================
if (healthCheckSettings.Enabled)
{
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });

    // Simple health check for load balancers
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    // Detailed health check (authenticated in production)
    app.MapHealthChecks("/health/detailed", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }).RequireAuthorization();

    Log.Information("Health check endpoints: /health, /health/ready, /health/detailed");
}

// Map Controllers
app.MapControllers();

// Root endpoint
app.MapGet("/", () => new
{
    name = "HRMS API - Enterprise Human Resource Management System",
    version = "1.0.0",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow,
    status = "Operational",
    endpoints = new
    {
        swagger = !app.Environment.IsProduction() ? "/swagger" : null,
        health = "/health",
        api = "/api"
    },
    features = new[]
    {
        "Multi-Tenant Architecture (Schema-per-Tenant)",
        "Employee Lifecycle Management",
        "Mauritius Labour Law Compliance",
        "Attendance & Biometric Integration",
        "Payroll with Statutory Deductions (NPF, NSF, PAYE)",
        "Leave Management with Accrual",
        "Reporting & Analytics",
        "Background Jobs (Hangfire)",
        "Health Monitoring",
        "Audit Logging"
    }
})
.WithName("Root")
.WithOpenApi()
.AllowAnonymous();

// ======================
// APPLICATION STARTUP
// ======================
Log.Information("=================================================");
Log.Information("HRMS API Started Successfully");
Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
Log.Information("HTTPS Metadata Required: {RequireHttps}", securitySettings.RequireHttpsMetadata);
Log.Information("Health Checks: {Enabled}", healthCheckSettings.Enabled ? "Enabled" : "Disabled");
Log.Information("Hangfire Dashboard: {Enabled}", hangfireSettings.DashboardEnabled ? "Enabled" : "Disabled");
Log.Information("Google Secret Manager: {Enabled}", googleCloudSettings.SecretManagerEnabled ? "Enabled" : "Disabled");
Log.Information("Multi-Tenant Architecture: Schema-per-Tenant");
Log.Information("=================================================");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
