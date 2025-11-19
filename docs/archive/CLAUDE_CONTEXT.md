# MorisHR - Project Context for Claude

## Project Overview
**Name**: MorisHR - Enterprise HR Management System
**Client**: Mauritius-based HR software
**Tech Stack**: ASP.NET Core 9.0, Angular 20, PostgreSQL, Hangfire
**Architecture**: Multi-tenant SaaS with schema isolation
**Status**: Active development, ~90% complete

## Core Features

### ✅ Completed Features
- Multi-tenant architecture with schema-based isolation (tenant_default, tenant_siraaj)
- Super Admin portal (tenant management, pricing tiers)
- JWT authentication with role-based authorization (SuperAdmin, HR, Manager, Employee)
- Employee management (comprehensive CRUD, validation, emergency contacts)
- Attendance tracking (clock in/out, corrections, machine integration)
- Leave management (applications, approvals, balance tracking, encashment)
- Payroll system (calculation engine, salary components, Mauritius tax compliance)
- **Timesheet management (auto-generation, approval workflow, adjustments)**
- Background jobs (Hangfire - salary processing, notifications, cleanup)
- Reports & PDF generation (QuestPDF integration)
- Industry sectors configuration (44 sectors, custom compliance rules)
- Angular 20 upgrade (standalone components, signals, modern patterns)
- Security hardening (SQL injection fixes, CORS, rate limiting)
- .NET 9.0 upgrade (latest framework, performance improvements)

### 🔄 In Progress
- Payroll integration with timesheets (read approved timesheets for salary calculation)
- Background jobs for timesheet auto-generation (weekly, reminders)
- Email notification system for timesheet events
- Timesheet reports and analytics

### ⏳ Planned Features
- Advanced reporting dashboard
- Mobile app integration
- Employee self-service portal enhancements
- Performance appraisal module
- Training & development tracking

## Recent Major Completion (2025-11-05)

### Timesheet Module - 100% Complete
**Backend Implementation:**
- 4 database tables: Timesheets, TimesheetEntries, TimesheetAdjustments, TimesheetComments
- TimesheetGenerationService: Auto-generates timesheets from attendance records
- TimesheetApprovalService: Manages Draft→Submit→Approve→Lock workflow
- TimesheetAdjustmentService: Handles corrections with audit trail
- TimesheetController: 20+ API endpoints (employee, manager, admin)
- Database migrations applied to both tenant schemas
- Mauritius labor law compliance: 40hrs/week (general), 45hrs/week (manufacturing/retail/hospitality)

**Frontend Implementation:**
- TypeScript models (215 lines): Complete type system with enums and helpers
- Angular service (292 lines): HTTP service with signal-based state management
- Employee timesheet list (512 lines): Stats dashboard, filtering, submit actions
- Employee timesheet detail (749 lines): Daily breakdown, hour totals, approval tracking
- Manager approval view (625 lines): Bulk operations, approve/reject workflow
- Routes configured for all views
- Angular build: ✅ Success (no errors)

**Total Implementation:** 2,393 lines of code

**Key Features:**
- Auto-generation from attendance records
- Approval workflow with status tracking
- Overtime calculation with Mauritius rules
- Adjustment system with audit trail
- Bulk approval operations for managers
- Comments and discussion threads
- Locked timesheets for payroll processing

## Database Schema

### Master Database (`hrms_master`)
- Tenants, PricingTiers (super admin data)

### Tenant Schemas (`tenant_default`, `tenant_siraaj`)
- Employees, Departments, EmergencyContacts
- Attendances, AttendanceMachines, AttendanceCorrections
- LeaveTypes, LeaveApplications, LeaveApprovals, LeaveBalances, LeaveEncashments
- **Timesheets, TimesheetEntries, TimesheetAdjustments, TimesheetComments** ⭐ NEW
- PayrollCycles, Payslips, SalaryComponents
- PublicHolidays
- TenantSectorConfigurations, TenantCustomComplianceRules

## Project Structure
```
HRAPP/
├── src/
│   ├── HRMS.API/                 # ASP.NET Core Web API
│   │   ├── Controllers/          # API endpoints
│   │   ├── Middleware/           # Auth, tenant resolution
│   │   └── BackgroundJobs/       # Hangfire jobs
│   ├── HRMS.Application/         # DTOs, services
│   │   └── DTOs/
│   │       └── TimesheetDtos/   # Timesheet request/response objects
│   ├── HRMS.Core/                # Domain models, interfaces
│   │   ├── Entities/Tenant/     # Tenant-specific entities
│   │   ├── Interfaces/          # Service contracts
│   │   └── Enums/               # System enums
│   └── HRMS.Infrastructure/      # Data access, services
│       ├── Data/                 # DbContexts, migrations
│       └── Services/             # Business logic implementations
│           ├── TimesheetGenerationService.cs
│           ├── TimesheetApprovalService.cs
│           └── TimesheetAdjustmentService.cs
└── hrms-frontend/                # Angular 20 application
    └── src/app/
        ├── core/
        │   ├── models/           # TypeScript interfaces
        │   │   └── timesheet.model.ts
        │   └── services/         # HTTP services
        │       └── timesheet.service.ts
        └── features/
            ├── employee/timesheets/   # Employee views
            │   ├── timesheet-list.component.*
            │   └── timesheet-detail.component.*
            └── tenant/timesheets/     # Manager views
                └── timesheet-approvals.component.*
```

## Key Technologies
- **Backend**: ASP.NET Core 9.0, Entity Framework Core 9.0
- **Frontend**: Angular 20, Material Design, Signals
- **Database**: PostgreSQL 14+ with multi-tenant schemas
- **Background Jobs**: Hangfire
- **PDF Generation**: QuestPDF
- **Validation**: FluentValidation
- **Authentication**: JWT tokens with HttpOnly cookies

## Environment Setup
- Development: PostgreSQL on localhost:5432
- Database: hrms_master
- Connection: Username=postgres, Password=postgres
- API: https://localhost:7001
- Frontend: http://localhost:4200

## Current Status Summary
- **Backend**: 95% complete
- **Frontend**: 90% complete
- **Integration**: 85% complete
- **Testing**: 70% complete
- **Documentation**: 80% complete

## Next Priority Tasks
1. ⚡ Integrate approved timesheets into payroll calculation logic
2. ⚡ Create Hangfire job for weekly timesheet auto-generation
3. ⚡ Implement email notifications for timesheet submit/approve/reject
4. ⚡ Build timesheet reports (summary, overtime analysis, discrepancies)
5. ⚡ End-to-end testing of complete timesheet workflow

## Important Notes for Claude
- Always use tenant context when working with tenant data
- Follow existing patterns: Services use interfaces, DTOs for API contracts
- Use signals for Angular state management (modern pattern)
- Timesheet hours must comply with Mauritius labor law
- All database changes require migrations for TenantDbContext
- Frontend uses standalone components (Angular 20 pattern)
- Material Design for consistent UI/UX
- Executive purple theme: #667eea to #764ba2

## Recent Decisions & Patterns
- **Timesheet Generation**: Automatic from attendance, triggered by Hangfire
- **Overtime Rules**: Sector-based (check TenantSectorConfiguration)
- **Approval Flow**: Draft → Submitted → Approved → Locked (no edit after approval)
- **Bulk Operations**: Enabled for manager efficiency
- **Audit Trail**: TimesheetAdjustments track all changes with reason

## Testing Notes
- Backend build: ✅ Success (0 errors)
- Frontend build: ✅ Success (0 errors)
- Database migrations: ✅ Applied to both tenant schemas
- Integration: ⏳ Needs end-to-end workflow testing

## Contact & Resources
- Changelog: /workspaces/HRAPP/CHANGELOG.md
- Deployment Guide: /workspaces/HRAPP/DEPLOYMENT_GUIDE.md
- Testing Guide: /workspaces/HRAPP/TESTING_AND_NEXT_STEPS.md
