# CLAUDE.md - AI Assistant Guide for HRAPP

> **Last Updated:** 2025-11-14
> **Version:** 1.0
> **Purpose:** Comprehensive guide for AI assistants working with the HRAPP codebase

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Codebase Architecture](#codebase-architecture)
3. [Development Workflows](#development-workflows)
4. [Key Conventions](#key-conventions)
5. [Testing Guidelines](#testing-guidelines)
6. [Security Considerations](#security-considerations)
7. [Common Tasks](#common-tasks)
8. [Important Notes for AI Assistants](#important-notes-for-ai-assistants)

---

## Project Overview

**HRAPP** is a production-grade, enterprise-level **Human Resource Management System (HRMS)** designed as a multi-tenant SaaS application with specific focus on **Mauritius labour law compliance**.

### Tech Stack

**Backend:**
- .NET 9.0 (C# 12)
- ASP.NET Core Web API
- PostgreSQL 14+ with Entity Framework Core 9
- Hangfire for background jobs
- SignalR for real-time updates
- Redis for distributed caching

**Frontend:**
- Angular 20
- TypeScript 5.9.2
- Angular Material 20
- SCSS
- SignalR client

### Core Features

- Employee lifecycle management (hire to retire)
- Multi-tenant architecture (schema-per-tenant isolation)
- Biometric attendance tracking with device integration
- Comprehensive payroll processing (Mauritius-specific)
- Leave management with accrual automation
- Timesheet tracking and approval workflows
- Compliance reporting (GDPR, SOX, Mauritius Labour Law)
- Advanced security (MFA, encryption, audit logging, anomaly detection)
- Real-time notifications
- Industry-specific compliance rules

### Architecture Pattern

**Clean Architecture (Onion Architecture):**

```
┌─────────────────────────────────────┐
│     HRMS.API (Presentation)         │  Controllers, Middleware, SignalR Hubs
├─────────────────────────────────────┤
│  HRMS.Application (Use Cases)       │  Business Logic, DTOs, Validators
├─────────────────────────────────────┤
│      HRMS.Core (Domain)             │  Entities, Interfaces, Enums
├─────────────────────────────────────┤
│  HRMS.Infrastructure (Services)     │  Data Access, External Services
└─────────────────────────────────────┘
       HRMS.BackgroundJobs              Hangfire Jobs
```

---

## Codebase Architecture

### Directory Structure

```
/home/user/HRAPP/
├── src/
│   ├── HRMS.API/              # Web API layer
│   │   ├── Controllers/       # 32 REST API controllers
│   │   ├── Middleware/        # Custom middleware (tenant resolution, rate limiting)
│   │   ├── Hubs/             # SignalR hubs (real-time)
│   │   ├── Filters/          # Global filters
│   │   └── appsettings.json  # Configuration
│   │
│   ├── HRMS.Core/            # Domain layer (no dependencies)
│   │   ├── Entities/
│   │   │   ├── Master/       # System-wide entities (Tenant, AdminUser, AuditLog)
│   │   │   └── Tenant/       # Tenant-isolated entities (Employee, Payroll, etc.)
│   │   ├── Interfaces/       # Service contracts
│   │   ├── DTOs/            # Data transfer objects
│   │   └── Enums/           # Business enumerations
│   │
│   ├── HRMS.Application/     # Application layer
│   │   ├── DTOs/            # Feature-specific DTOs
│   │   ├── Validators/      # FluentValidation validators (11+)
│   │   └── Services/        # Service interfaces
│   │
│   ├── HRMS.Infrastructure/  # Infrastructure layer
│   │   ├── Data/
│   │   │   ├── MasterDbContext.cs        # Master database context
│   │   │   ├── TenantDbContext.cs        # Tenant database context
│   │   │   └── Migrations/               # EF Core migrations
│   │   └── Services/                     # 45+ service implementations
│   │
│   └── HRMS.BackgroundJobs/  # Background processing
│       └── Jobs/             # 7 Hangfire recurring jobs
│
├── hrms-frontend/            # Angular 20 application
│   └── src/app/
│       ├── core/             # Singleton services, guards, interceptors
│       │   ├── services/     # 28 core services
│       │   ├── guards/       # Auth, role, subdomain guards
│       │   ├── interceptors/ # HTTP interceptors
│       │   └── models/       # TypeScript interfaces
│       │
│       ├── features/         # Feature modules (lazy loaded)
│       │   ├── admin/        # SuperAdmin portal (13 sub-features)
│       │   ├── tenant/       # Tenant/HR portal (12 sub-features)
│       │   ├── employee/     # Employee self-service (7 features)
│       │   ├── auth/         # Authentication flows
│       │   └── marketing/    # Landing page
│       │
│       └── shared/           # Shared components & layouts
│
├── tests/HRMS.Tests/         # xUnit tests
├── docs/                     # Technical documentation (100+ files)
├── scripts/                  # Deployment and migration scripts
├── sql/                      # Database scripts
└── monitoring/               # Monitoring configurations
```

### Database Architecture

**Multi-Tenant Schema-per-Tenant Strategy:**

```
PostgreSQL Server
├── Master Database (Schema: master)
│   ├── Tenants               # Customer organizations
│   ├── AdminUsers            # SuperAdmin users
│   ├── AuditLogs            # System-wide audit trail
│   ├── SecurityAlerts        # Threat detection logs
│   ├── DetectedAnomalies     # Anomaly events
│   ├── LegalHolds           # Compliance holds
│   └── RefreshTokens        # JWT refresh tokens
│
└── Tenant Databases (Schema: tenant_{id})
    ├── Employees            # Employee records (PII encrypted)
    ├── Departments          # Organizational units
    ├── Attendance           # Daily attendance
    ├── LeaveApplications    # Leave requests
    ├── PayrollCycles        # Payroll runs
    ├── Timesheets          # Time tracking
    └── BiometricDevices    # Device management
```

### API Structure

**32 Controllers organized by domain:**

| Domain | Controller | Purpose |
|--------|-----------|---------|
| **Auth** | `AuthController` | Login, registration, MFA, token refresh |
| **Auth** | `MfaController` | MFA enrollment and verification |
| **SuperAdmin** | `TenantsController` | Tenant CRUD operations |
| **SuperAdmin** | `AuditLogController` | System audit trail |
| **SuperAdmin** | `SecurityAlertController` | Security monitoring |
| **SuperAdmin** | `AnomalyDetectionController` | Threat detection |
| **SuperAdmin** | `ComplianceReportsController` | SOX/GDPR reports |
| **HR** | `EmployeesController` | Employee management |
| **HR** | `EmployeeDraftsController` | Draft employee approval workflow |
| **HR** | `DepartmentController` | Department management |
| **HR** | `AttendanceController` | Attendance tracking |
| **HR** | `LeavesController` | Leave applications |
| **HR** | `PayrollController` | Payroll processing |
| **HR** | `TimesheetController` | Timesheet management |
| **HR** | `ReportsController` | Analytics & reports |
| **Devices** | `DeviceWebhookController` | Biometric device webhooks |
| **Devices** | `BiometricDevicesController` | Device management |
| **Utilities** | `AddressLookupController` | Mauritius address lookup |

**API Conventions:**
- Base path: `/api/{resource}`
- RESTful design (GET, POST, PUT, DELETE)
- JWT Bearer authentication required (except auth endpoints)
- Tenant isolation via `TenantResolutionMiddleware`
- Consistent response DTOs
- Health checks: `/health`, `/health/ready`, `/health/detailed`

---

## Development Workflows

### Initial Setup

**Prerequisites:**
- .NET 9.0 SDK
- Node.js 18+
- PostgreSQL 14+
- Redis (optional)
- Visual Studio 2022 or VS Code

**Backend Setup:**
```bash
# 1. Clone repository
git clone <repo-url>
cd HRAPP

# 2. Restore dependencies
cd src/HRMS.API
dotnet restore

# 3. Configure database
# Edit src/HRMS.API/appsettings.json
# Update ConnectionStrings:MasterConnection and TenantConnection

# 4. Apply migrations (auto-applies in Development)
dotnet ef database update --context MasterDbContext

# 5. Run application
dotnet run
# API runs on https://localhost:5090
```

**Frontend Setup:**
```bash
# 1. Navigate to frontend
cd hrms-frontend

# 2. Install dependencies
npm install

# 3. Configure environment
# Edit src/environments/environment.ts
# Set apiUrl to backend URL

# 4. Run development server
ng serve
# App runs on http://localhost:4200
```

### Making Changes

**Backend Development:**

1. **Adding a new entity:**
   - Create entity class in `HRMS.Core/Entities/Master/` or `Tenant/`
   - Add `DbSet<T>` to appropriate `DbContext`
   - Create migration: `dotnet ef migrations add <Name> --context <Context>`
   - Create DTOs in `HRMS.Application/DTOs/`
   - Create service interface in `HRMS.Application/Services/`
   - Implement service in `HRMS.Infrastructure/Services/`
   - Register in DI container (`Program.cs`)
   - Create controller in `HRMS.API/Controllers/`

2. **Adding a new API endpoint:**
   - Create/update controller in `HRMS.API/Controllers/`
   - Add authorization: `[Authorize(Roles = "HR")]` or `[RequireTenant]`
   - Create DTOs with FluentValidation validators
   - Implement business logic in service layer
   - Test with Swagger UI (`/swagger`)

3. **Adding a background job:**
   - Create job class in `HRMS.BackgroundJobs/Jobs/`
   - Implement `IJob` interface
   - Register in `Program.cs` with schedule
   - Monitor via Hangfire dashboard (`/hangfire`)

**Frontend Development:**

1. **Adding a new feature:**
   - Create feature module in `src/app/features/`
   - Use standalone components
   - Create service in `core/services/` if shared
   - Add routes with appropriate guards
   - Use Angular Material components

2. **Adding a new page:**
   - Create component: `ng generate component features/<feature>/<page>`
   - Add route in feature routing module
   - Implement TypeScript logic with RxJS
   - Use reactive forms for user input
   - Call API services via HTTP

### Testing

**Backend Tests:**
```bash
cd tests/HRMS.Tests
dotnet test
```

**Test Structure:**
- Unit tests for service layer
- Mock external dependencies with Moq
- Use in-memory database for EF Core tests
- FluentAssertions for readable assertions

**Shell Scripts for Testing:**
```bash
./test-setup.sh              # Environment setup
./integration-tests.sh       # Full integration suite
./test-audit-system.sh       # Audit log testing
./test-mfa-complete.sh       # MFA workflow
```

### Database Migrations

**Creating Migrations:**
```bash
# Master database
cd src/HRMS.API
dotnet ef migrations add <MigrationName> --context MasterDbContext --output-dir ../HRMS.Infrastructure/Data/Migrations/Master

# Tenant database
dotnet ef migrations add <MigrationName> --context TenantDbContext --output-dir ../HRMS.Infrastructure/Data/Migrations/Tenant
```

**Applying Migrations:**
```bash
# Development (auto-applies on startup)
dotnet run

# Production (manual)
./scripts/deploy-migrations-production.sh

# Rollback
./scripts/rollback-migrations.sh
```

**Migration Best Practices:**
- Test migrations on staging first
- Always create backups before production migrations
- Include both Up and Down methods
- Test rollback procedures
- Use descriptive migration names

### Deployment

**Build Production:**
```bash
# Backend
dotnet publish src/HRMS.API -c Release -o ./publish

# Frontend
cd hrms-frontend
ng build --configuration=production
```

**Deployment Scripts:**
- `scripts/deploy-migrations-production.sh` - Production deployment
- `scripts/deploy-migrations-staging.sh` - Staging deployment
- `scripts/verify-migrations.sh` - Migration validation
- `scripts/post-migration-health-check.sh` - Health verification
- `scripts/monitor-database-health.sh` - Database monitoring

**Deployment Platforms:**
- Primary: Google Cloud Platform
- Database: Cloud SQL for PostgreSQL
- Container: Cloud Run
- Secrets: Google Secret Manager
- Storage: Cloud Storage

---

## Key Conventions

### Naming Conventions

**C# Code:**
- **Classes:** PascalCase (`EmployeeService`, `PayrollController`)
- **Interfaces:** `I` prefix + PascalCase (`IEmployeeService`)
- **Methods:** PascalCase (`GetEmployeeById`, `CalculatePayroll`)
- **Parameters:** camelCase (`employeeId`, `startDate`)
- **Private fields:** `_` prefix + camelCase (`_context`, `_logger`)
- **Constants:** PascalCase (`MaxAttempts`, `DefaultTimeout`)
- **DTOs:** Entity + `Dto` suffix (`EmployeeDto`, `CreateEmployeeDto`)
- **Validators:** DTO + `Validator` suffix (`EmployeeDtoValidator`)
- **Background jobs:** Purpose + `Job` suffix (`LeaveAccrualJob`)

**TypeScript/Angular:**
- **Components:** kebab-case files, PascalCase class (`employee-list.component.ts` → `EmployeeListComponent`)
- **Services:** kebab-case files, PascalCase class with `Service` suffix (`employee.service.ts` → `EmployeeService`)
- **Interfaces:** PascalCase, optional `I` prefix (`Employee` or `IEmployee`)
- **Methods:** camelCase (`getEmployees`, `calculateTotal`)
- **Properties:** camelCase (`firstName`, `employeeId`)
- **Constants:** UPPER_SNAKE_CASE (`API_BASE_URL`, `MAX_RETRIES`)

**Database:**
- **Tables:** PascalCase, plural (`Employees`, `PayrollCycles`)
- **Columns:** PascalCase (`EmployeeId`, `FirstName`)
- **Foreign keys:** `{Entity}Id` (`EmployeeId`, `DepartmentId`)
- **Indexes:** `IX_{Table}_{Column}` (`IX_Employees_Email`)
- **Schemas:** lowercase (`master`, `tenant_1`)

### Code Style

**C# Conventions:**
- Use nullable reference types (`string?` for nullable)
- Async methods end with `Async` suffix
- Use expression-bodied members for simple properties/methods
- Constructor injection for dependencies
- Guard clauses at method start
- Use `var` for obvious types
- 4-space indentation

**TypeScript Conventions:**
- Strict mode enabled
- Use `const` by default, `let` when needed
- Arrow functions for callbacks
- RxJS for async operations
- 2-space indentation
- Use interfaces for data models

### API Conventions

**Request/Response:**
- Use DTOs for all requests/responses (never expose entities)
- camelCase JSON properties
- Ignore null values in responses
- Standard error format:
  ```json
  {
    "statusCode": 400,
    "message": "Validation failed",
    "errors": ["Field X is required"]
  }
  ```

**Authentication:**
- JWT Bearer token in `Authorization` header
- Token format: `Bearer {token}`
- Refresh token in secure HTTP-only cookie
- MFA required for SuperAdmin

**Pagination:**
- Query parameters: `pageNumber`, `pageSize`
- Response includes: `data`, `totalCount`, `pageNumber`, `pageSize`

**Filtering/Sorting:**
- Query parameters: `sortBy`, `sortOrder`, `filter`
- Example: `/api/employees?sortBy=lastName&sortOrder=asc`

### Error Handling

**Backend:**
- Use custom exceptions:
  - `NotFoundException` - Resource not found (404)
  - `BusinessException` - Business rule violation (400)
  - `UnauthorizedException` - Auth failure (401)
- Global exception middleware handles all exceptions
- Include correlation ID in error responses
- Log all errors with Serilog

**Frontend:**
- HTTP interceptor handles global errors
- Display user-friendly error messages
- Log errors to console (development)
- Show toast notifications for errors

### Validation

**Backend (FluentValidation):**
```csharp
public class EmployeeDtoValidator : AbstractValidator<EmployeeDto>
{
    public EmployeeDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
```

**Frontend (Reactive Forms):**
```typescript
this.form = this.fb.group({
  firstName: ['', [Validators.required, Validators.maxLength(50)]],
  email: ['', [Validators.required, Validators.email]]
});
```

### Logging

**Structured Logging with Serilog:**
```csharp
_logger.LogInformation(
    "Employee {EmployeeId} created by {UserId} in tenant {TenantId}",
    employee.Id,
    currentUser.Id,
    currentUser.TenantId
);
```

**Log Levels:**
- **Debug:** Detailed diagnostic info (development only)
- **Information:** General flow tracking
- **Warning:** Unexpected but recoverable issues
- **Error:** Failures that need attention
- **Fatal:** Critical failures requiring immediate action

**PII Masking:**
- Never log sensitive data (passwords, SSN, credit cards)
- Mask email addresses: `u***@example.com`
- Mask phone numbers: `****5678`

### Multi-Tenancy

**Tenant Resolution:**
- Extract tenant from subdomain: `{tenant}.example.com`
- `TenantResolutionMiddleware` sets tenant context
- All tenant operations use `TenantDbContext`
- Schema switching: `SET search_path TO tenant_{id}`

**Tenant Isolation:**
- Never share data between tenants
- All queries automatically scoped to current tenant
- Foreign keys cannot cross tenant boundaries
- Audit logs include tenant ID

**SuperAdmin vs Tenant:**
- SuperAdmin: Works with `MasterDbContext`, no tenant context
- HR/Employee: Works with `TenantDbContext`, requires tenant context
- Use `[RequireTenant]` attribute on tenant endpoints

---

## Testing Guidelines

### Test Structure

**xUnit Test Pattern:**
```csharp
public class EmployeeServiceTests
{
    private readonly Mock<TenantDbContext> _contextMock;
    private readonly EmployeeService _service;

    public EmployeeServiceTests()
    {
        // Arrange - Setup
        _contextMock = new Mock<TenantDbContext>();
        _service = new EmployeeService(_contextMock.Object);
    }

    [Fact]
    public async Task GetEmployeeById_ExistingId_ReturnsEmployee()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee { Id = employeeId };
        _contextMock.Setup(x => x.Employees.FindAsync(employeeId))
            .ReturnsAsync(employee);

        // Act
        var result = await _service.GetEmployeeByIdAsync(employeeId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(employeeId);
    }
}
```

### What to Test

**Priority Areas:**
1. **Business logic** in service layer
2. **Validation rules** (FluentValidation)
3. **Security features** (authentication, authorization, encryption)
4. **Critical paths** (payroll calculation, leave accrual, attendance processing)
5. **Edge cases** (null inputs, boundary values)
6. **Integration tests** for database operations

**Low Priority:**
- Controllers (thin layer)
- DTOs (data classes)
- Simple CRUD operations

### Test Data

**Use Builder Pattern:**
```csharp
var employee = new EmployeeBuilder()
    .WithFirstName("John")
    .WithLastName("Doe")
    .WithEmail("john.doe@example.com")
    .Build();
```

**In-Memory Database:**
```csharp
var options = new DbContextOptionsBuilder<TenantDbContext>()
    .UseInMemoryDatabase(databaseName: "TestDb")
    .Options;
var context = new TenantDbContext(options);
```

### Running Tests

```bash
# Run all tests
cd tests/HRMS.Tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~EmployeeServiceTests"

# Run with coverage
dotnet test /p:CollectCoverage=true

# Integration tests
./integration-tests.sh
```

---

## Security Considerations

### Authentication & Authorization

**JWT Authentication:**
- Access token: 15-minute expiration
- Refresh token: 7-day expiration, stored in HTTP-only cookie
- Token includes: `userId`, `tenantId`, `role`, `email`
- Validate token on every request

**Multi-Factor Authentication (MFA):**
- TOTP-based (Google Authenticator, Authy)
- Required for SuperAdmin users
- Backup codes for account recovery
- QR code generation for enrollment

**Password Security:**
- Argon2 hashing (memory-hard, resistant to GPU attacks)
- Minimum 8 characters, complexity requirements
- Password history (prevent reuse)
- Account lockout after 5 failed attempts

**Role-Based Access Control:**
- Roles: `SuperAdmin`, `HR`, `Employee`
- Use `[Authorize(Roles = "HR")]` on controllers
- Use guards in Angular: `canActivate: [hrGuard]`
- Fine-grained permissions via policies

### Data Protection

**Encryption at Rest:**
- Column-level AES-256-GCM encryption
- Encrypted fields: SSN, salary, bank accounts, NID
- Automatic via EF Core value converters
- Key rotation support via Secret Manager

**Encryption in Transit:**
- HTTPS enforced (HSTS enabled)
- TLS 1.2+ only
- Certificate pinning recommended

**PII Protection:**
- Minimize PII collection
- Encrypt sensitive fields
- Mask PII in logs and UI
- Right to erasure (soft delete)

### Audit Logging

**Comprehensive Audit Trail:**
- All data modifications logged
- Immutable logs (database trigger prevents tampering)
- SHA256 checksums for verification
- Includes: `who`, `what`, `when`, `where`, `why`

**Audit Fields:**
- `UserId` - Who performed action
- `TenantId` - Which tenant (null for SuperAdmin)
- `Action` - Create/Update/Delete
- `EntityType` - Entity name
- `EntityId` - Record ID
- `Changes` - Before/after JSON
- `Timestamp` - UTC timestamp
- `IpAddress` - Source IP
- `CorrelationId` - Request tracing

**Viewing Audit Logs:**
- SuperAdmin: `/api/audit-logs` (all tenants)
- HR: `/api/tenant-audit-logs` (own tenant only)

### Security Features

**Rate Limiting:**
- IP-based throttling (configurable per endpoint)
- Auto-blacklisting for abuse
- Redis-backed for distributed deployment
- Configured in `appsettings.json`

**Anomaly Detection:**
- Failed login attempts (5+ in 15 minutes)
- Off-hours access patterns
- Unusual IP addresses
- Mass data exports
- Suspicious admin actions

**Security Alerts:**
- Real-time notifications via:
  - Email (SMTP2GO)
  - SMS (Twilio)
  - Slack webhooks
  - SIEM integration
- Alert types: Authentication, Authorization, DataAccess, SystemSecurity

**API Key Security:**
- Device API keys for biometric devices
- SHA256 hashing (never stored plaintext)
- Rotation support
- Per-device key isolation

### Compliance

**GDPR Compliance:**
- Data encryption
- Audit logging
- Right to access (data export)
- Right to erasure (soft delete)
- Data retention policies
- Privacy by design

**SOX Compliance:**
- Immutable audit trail
- Segregation of duties
- Change management tracking
- Access controls
- Regular compliance reports

**Mauritius Labour Law:**
- Industry-specific compliance rules
- Statutory deduction calculations
- Leave accrual per law
- Working hours regulations

### Security Best Practices for Development

**DO:**
- Use parameterized queries (EF Core handles this)
- Validate all user input (FluentValidation)
- Sanitize output (Angular handles XSS)
- Use HTTPS everywhere
- Store secrets in Secret Manager (never in code)
- Implement least privilege access
- Log security events

**DON'T:**
- Log sensitive data (passwords, tokens, PII)
- Trust user input without validation
- Expose internal errors to users
- Use weak cryptography
- Store secrets in `appsettings.json` (use User Secrets/Secret Manager)
- Disable security features in production
- Share API keys or credentials

---

## Common Tasks

### Adding a New Entity

**Step-by-step guide:**

1. **Create entity class** in `HRMS.Core/Entities/Tenant/`:
```csharp
public class JobTitle
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

2. **Add to DbContext** in `HRMS.Infrastructure/Data/TenantDbContext.cs`:
```csharp
public DbSet<JobTitle> JobTitles => Set<JobTitle>();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<JobTitle>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        entity.HasQueryFilter(e => !e.IsDeleted); // Soft delete
        entity.HasOne(e => e.Department)
              .WithMany()
              .HasForeignKey(e => e.DepartmentId);
    });
}
```

3. **Create migration**:
```bash
cd src/HRMS.API
dotnet ef migrations add AddJobTitle --context TenantDbContext
```

4. **Create DTOs** in `HRMS.Application/DTOs/JobTitle/`:
```csharp
public class JobTitleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid DepartmentId { get; set; }
}

public class CreateJobTitleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid DepartmentId { get; set; }
}
```

5. **Create validator** in `HRMS.Application/Validators/`:
```csharp
public class CreateJobTitleDtoValidator : AbstractValidator<CreateJobTitleDto>
{
    public CreateJobTitleDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepartmentId).NotEmpty();
    }
}
```

6. **Create service interface** in `HRMS.Core/Interfaces/`:
```csharp
public interface IJobTitleService
{
    Task<List<JobTitleDto>> GetAllJobTitlesAsync();
    Task<JobTitleDto?> GetJobTitleByIdAsync(Guid id);
    Task<JobTitleDto> CreateJobTitleAsync(CreateJobTitleDto dto);
    Task UpdateJobTitleAsync(Guid id, CreateJobTitleDto dto);
    Task DeleteJobTitleAsync(Guid id);
}
```

7. **Implement service** in `HRMS.Infrastructure/Services/`:
```csharp
public class JobTitleService : IJobTitleService
{
    private readonly TenantDbContext _context;

    public JobTitleService(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<List<JobTitleDto>> GetAllJobTitlesAsync()
    {
        return await _context.JobTitles
            .Select(jt => new JobTitleDto
            {
                Id = jt.Id,
                Name = jt.Name,
                Description = jt.Description,
                DepartmentId = jt.DepartmentId
            })
            .ToListAsync();
    }

    // ... other methods
}
```

8. **Register in DI** in `src/HRMS.API/Program.cs`:
```csharp
builder.Services.AddScoped<IJobTitleService, JobTitleService>();
```

9. **Create controller** in `HRMS.API/Controllers/`:
```csharp
[ApiController]
[Route("api/job-titles")]
[Authorize(Roles = "HR")]
[RequireTenant]
public class JobTitlesController : ControllerBase
{
    private readonly IJobTitleService _service;

    public JobTitlesController(IJobTitleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<JobTitleDto>>> GetAll()
    {
        var jobTitles = await _service.GetAllJobTitlesAsync();
        return Ok(jobTitles);
    }

    [HttpPost]
    public async Task<ActionResult<JobTitleDto>> Create([FromBody] CreateJobTitleDto dto)
    {
        var jobTitle = await _service.CreateJobTitleAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = jobTitle.Id }, jobTitle);
    }

    // ... other endpoints
}
```

### Adding a New API Endpoint

**Example: Add attendance summary endpoint**

1. **Add method to service interface**:
```csharp
public interface IAttendanceService
{
    Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(Guid employeeId, DateTime month);
}
```

2. **Implement service method**:
```csharp
public async Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(Guid employeeId, DateTime month)
{
    var startDate = new DateTime(month.Year, month.Month, 1);
    var endDate = startDate.AddMonths(1).AddDays(-1);

    var records = await _context.Attendances
        .Where(a => a.EmployeeId == employeeId
                 && a.Date >= startDate
                 && a.Date <= endDate)
        .ToListAsync();

    return new AttendanceSummaryDto
    {
        TotalDays = records.Count,
        PresentDays = records.Count(r => r.Status == AttendanceStatus.Present),
        AbsentDays = records.Count(r => r.Status == AttendanceStatus.Absent),
        LateDays = records.Count(r => r.IsLate)
    };
}
```

3. **Add controller endpoint**:
```csharp
[HttpGet("{employeeId}/summary")]
public async Task<ActionResult<AttendanceSummaryDto>> GetSummary(
    Guid employeeId,
    [FromQuery] DateTime month)
{
    var summary = await _attendanceService.GetAttendanceSummaryAsync(employeeId, month);
    return Ok(summary);
}
```

### Adding a Background Job

**Example: Monthly report generation**

1. **Create job class** in `HRMS.BackgroundJobs/Jobs/`:
```csharp
public class MonthlyReportJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MonthlyReportJob> _logger;

    public MonthlyReportJob(IServiceProvider serviceProvider, ILogger<MonthlyReportJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting monthly report generation");

        using var scope = _serviceProvider.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();

        var tenants = await GetAllActiveTenants();

        foreach (var tenant in tenants)
        {
            await GenerateTenantReport(tenant, reportService);
        }

        _logger.LogInformation("Completed monthly report generation");
    }
}
```

2. **Register in Program.cs**:
```csharp
// Schedule to run on 1st of each month at 2 AM
RecurringJob.AddOrUpdate<MonthlyReportJob>(
    "monthly-report",
    job => job.ExecuteAsync(),
    "0 2 1 * *", // Cron: At 02:00 on day 1
    TimeZoneInfo.FindSystemTimeZoneById("Indian/Mauritius")
);
```

### Adding a Frontend Feature

**Example: Job Title management page**

1. **Generate component**:
```bash
cd hrms-frontend
ng generate component features/tenant/job-titles
```

2. **Create service** in `core/services/job-title.service.ts`:
```typescript
@Injectable({ providedIn: 'root' })
export class JobTitleService {
  private apiUrl = `${environment.apiUrl}/job-titles`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<JobTitleDto[]> {
    return this.http.get<JobTitleDto[]>(this.apiUrl);
  }

  create(dto: CreateJobTitleDto): Observable<JobTitleDto> {
    return this.http.post<JobTitleDto>(this.apiUrl, dto);
  }
}
```

3. **Implement component** in `job-titles.component.ts`:
```typescript
export class JobTitlesComponent implements OnInit {
  jobTitles: JobTitleDto[] = [];
  loading = false;

  constructor(private jobTitleService: JobTitleService) {}

  ngOnInit(): void {
    this.loadJobTitles();
  }

  loadJobTitles(): void {
    this.loading = true;
    this.jobTitleService.getAll().subscribe({
      next: (data) => {
        this.jobTitles = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading job titles', error);
        this.loading = false;
      }
    });
  }
}
```

4. **Create template** in `job-titles.component.html`:
```html
<div class="container">
  <h2>Job Titles</h2>

  <button mat-raised-button color="primary" (click)="openCreateDialog()">
    Add Job Title
  </button>

  <mat-table [dataSource]="jobTitles" *ngIf="!loading">
    <ng-container matColumnDef="name">
      <mat-header-cell *matHeaderCellDef>Name</mat-header-cell>
      <mat-cell *matCellDef="let jobTitle">{{jobTitle.name}}</mat-cell>
    </ng-container>

    <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
    <mat-row *matRowDef="let row; columns: displayedColumns;"></mat-row>
  </mat-table>
</div>
```

5. **Add route** in tenant routing:
```typescript
{
  path: 'job-titles',
  component: JobTitlesComponent,
  canActivate: [hrGuard]
}
```

---

## Important Notes for AI Assistants

### Critical Reminders

**1. Multi-Tenancy is Non-Negotiable:**
- ALWAYS consider tenant isolation when modifying data
- Use `TenantDbContext` for tenant-specific operations
- Use `MasterDbContext` only for SuperAdmin/system-wide operations
- Never query across tenant boundaries
- Test tenant isolation thoroughly

**2. Security First:**
- Never log sensitive data (passwords, tokens, SSN, NID)
- Always validate user input with FluentValidation
- Use `[Authorize]` on all non-public endpoints
- Encrypt sensitive PII fields (use existing patterns)
- Review OWASP Top 10 before adding features

**3. Data Integrity:**
- Use transactions for multi-step operations
- Implement soft deletes (set `IsDeleted = true`)
- Add audit fields (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `ModifiedBy`)
- Validate foreign key relationships
- Use unique constraints on business keys

**4. Performance Considerations:**
- Use async/await for all I/O operations
- Implement pagination for list endpoints
- Add database indexes for frequently queried columns
- Use `.AsNoTracking()` for read-only queries
- Consider caching for frequently accessed data

**5. Consistency:**
- Follow existing naming conventions
- Match code style of surrounding code
- Use existing patterns (don't reinvent)
- Add XML documentation for public APIs
- Update related tests when modifying logic

### When Adding Features

**Ask these questions:**
1. Does this respect tenant isolation?
2. Is this properly authorized?
3. Are sensitive fields encrypted?
4. Is this auditable (who, what, when)?
5. Is input validated?
6. Are errors handled gracefully?
7. Is this tested?
8. Does this follow existing patterns?

### Common Pitfalls to Avoid

**DON'T:**
- Expose database entities directly via API (use DTOs)
- Forget to add `[RequireTenant]` on tenant endpoints
- Hard-code configuration values (use `appsettings.json`)
- Return different error responses for security reasons (e.g., "user not found" vs "password incorrect")
- Modify audit logs (they're immutable)
- Use `.Result` or `.Wait()` on async operations (causes deadlocks)
- Forget to dispose resources (use `using` statements)
- Skip validation on API inputs

**DO:**
- Use dependency injection for all services
- Follow single responsibility principle
- Write tests for business logic
- Log important operations
- Handle exceptions appropriately
- Use meaningful variable names
- Comment complex logic
- Update documentation when needed

### File Locations Quick Reference

**Common files to modify:**

| Task | File Location |
|------|---------------|
| Add entity | `src/HRMS.Core/Entities/Tenant/` or `Master/` |
| Add DTO | `src/HRMS.Application/DTOs/{Feature}/` |
| Add validator | `src/HRMS.Application/Validators/` |
| Add service interface | `src/HRMS.Core/Interfaces/` |
| Implement service | `src/HRMS.Infrastructure/Services/` |
| Add controller | `src/HRMS.API/Controllers/` |
| Add migration | `dotnet ef migrations add {Name}` |
| Register in DI | `src/HRMS.API/Program.cs` |
| Add background job | `src/HRMS.BackgroundJobs/Jobs/` |
| Configure settings | `src/HRMS.API/appsettings.json` |
| Add Angular component | `hrms-frontend/src/app/features/` |
| Add Angular service | `hrms-frontend/src/app/core/services/` |
| Add route guard | `hrms-frontend/src/app/core/guards/` |

### Understanding the Data Flow

**Typical request flow:**

```
1. HTTP Request → Angular Component
2. Component → Angular Service (HTTP)
3. HTTP → .NET API Controller
4. Controller → Middleware Pipeline
   - TenantResolutionMiddleware (extracts tenant)
   - AuthenticationMiddleware (validates JWT)
   - RateLimitingMiddleware (throttles requests)
5. Controller → FluentValidation (validates input)
6. Controller → Service Layer (business logic)
7. Service → DbContext (data access)
8. DbContext → PostgreSQL Database
9. Response ← (reverse the flow)
```

### Mauritius-Specific Context

**Important for this application:**
- **Industry Sectors:** 11 predefined sectors (Manufacturing, Tourism, etc.)
- **Geographic Data:** District → Village → Postal Code hierarchy
- **Statutory Deductions:**
  - NPF (National Pension Fund) - 9% employee, 6% employer
  - NSF (National Savings Fund) - 1% employee
  - PAYE (Pay As You Earn) - Progressive tax brackets
- **Leave Entitlements:** Based on Mauritius Workers' Rights Act
- **Public Holidays:** Mauritian national holidays
- **Working Hours:** 45-hour work week standard

### Version Information

**Current versions (as of last update):**
- .NET: 9.0 (SDK 9.0.306)
- C#: 12
- Entity Framework Core: 9.0
- Angular: 20.3.0
- TypeScript: 5.9.2
- PostgreSQL: 14+
- Node.js: 18+

### Getting Help

**Resources:**
- **Documentation:** See `docs/` directory (100+ files)
- **API Docs:** Swagger UI at `/swagger` when running
- **Database Schema:** See migrations in `HRMS.Infrastructure/Data/Migrations/`
- **Example Code:** Browse existing controllers/services for patterns

### Key Design Decisions

**Why these choices were made:**

1. **Schema-per-Tenant:** Better isolation, easier compliance, independent scaling
2. **Clean Architecture:** Testability, maintainability, technology independence
3. **EF Core over Dapper:** Type safety, migrations, LINQ, less boilerplate
4. **FluentValidation:** Separation of concerns, reusable validators
5. **JWT + Refresh Tokens:** Stateless auth, secure, scalable
6. **Hangfire:** Reliable background processing, monitoring dashboard
7. **SignalR:** Real-time updates without polling
8. **Angular Material:** Consistent UI, accessibility, responsive
9. **Argon2:** Modern password hashing resistant to GPU attacks
10. **Column Encryption:** Granular security, compliance with data protection laws

---

## Summary

This HRMS application is a **production-ready, enterprise-grade system** designed for **Fortune 500 deployments**. It emphasizes:

- **Security:** Multi-layered defense, encryption, MFA, audit logging
- **Compliance:** GDPR, SOX, Mauritius Labour Law
- **Scalability:** Multi-tenant architecture, background jobs, caching
- **Maintainability:** Clean architecture, comprehensive tests, extensive documentation
- **User Experience:** Real-time updates, responsive design, intuitive UI

When working with this codebase:
- **Respect the architecture** - Don't bypass layers
- **Follow the patterns** - Consistency is key
- **Test thoroughly** - Especially security and multi-tenancy
- **Document changes** - Help future developers
- **Think security first** - This system handles sensitive employee data

---

**Last Updated:** 2025-11-14
**Maintained By:** AI Assistant
**Version:** 1.0

For questions or clarifications, refer to the extensive documentation in the `docs/` directory or examine existing code patterns.
