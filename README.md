# HRMS - Multi-Tenant Human Resource Management System

**Enterprise-Grade HRMS** for Mauritius businesses with biometric integration, payroll processing, and comprehensive compliance features.

![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?logo=.net)
![Angular 20](https://img.shields.io/badge/Angular-20.3-DD0031?logo=angular)
![PostgreSQL 16](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)
![License](https://img.shields.io/badge/License-Proprietary-red)

---

## 🚀 Quick Start

```bash
# Backend (.NET 9.0 API)
cd src/HRMS.API
dotnet restore
dotnet run

# Frontend (Angular 20)
cd hrms-frontend
npm install
npm start

# Visit: http://localhost:4200
```

**📖 Full Setup Guide:** [docs/guides/SYSTEM_SETUP_GUIDE.md](docs/guides/SYSTEM_SETUP_GUIDE.md)

---

## ⚠️ CRITICAL: Issues to Fix

**65 issues identified** - See [COMPREHENSIVE_ISSUES_AND_FIXES_REQUIRED.md](COMPREHENSIVE_ISSUES_AND_FIXES_REQUIRED.md)

### P0 Critical Bugs (Fix This Week):
1. ✅ **Frontend build broken** - FIXED (dependencies installed, SASS errors resolved)
2. 🔴 **DateTime precision loss** - Audit checksum failures in production
3. 🔴 **DbContext anti-pattern** - 5-15ms overhead per request
4. 🔴 **Connection pool exhaustion** - Need 1500 connections for 100 tenants
5. 🔴 **TenantService race condition** - Cross-tenant data leak risk
6. 🔴 **ThreadPool exhaustion** - Task.Run in request scope

**Total Cost to Fix:** $75,000 (500 developer hours)
**Timeline:** 3-4 months with 2 developers + 1 DevOps

---

## 📚 Documentation

All documentation has been organized into topic-based folders:

| Category | Description | Files |
|----------|-------------|-------|
| [📦 Deployment](docs/deployment/) | Production deployment, migrations, Cloud Run | 14 |
| [💾 Database](docs/database/) | Schema, indexes, performance, backups | 16 |
| [🔒 Security](docs/security/) | MFA, encryption, audit logging, compliance | 12 |
| [📖 Guides](docs/guides/) | Quick starts, runbooks, references | 17 |
| [🏗️ Architecture](docs/architecture/) | System design, patterns, flows | 6 |
| [🎨 Frontend](docs/frontend/) | Angular components, services, UI | 9 |
| [🧪 Testing](docs/testing/) | Test plans, results, QA procedures | 9 |
| [📁 Archive](docs/archive/) | ⚠️ Obsolete historical docs | 73 |

**Master Index:** [docs/README.md](docs/README.md)

---

## 🏢 System Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Angular 20 Frontend                   │
│          (tenant1.morishr.com, tenant2.morishr.com)     │
└─────────────────────┬───────────────────────────────────┘
                      │ HTTPS/JWT
┌─────────────────────▼───────────────────────────────────┐
│              .NET 9.0 Web API (Cloud Run)               │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────┐ │
│  │ Controllers  │  │  Middleware  │  │  Background   │ │
│  │  (32 APIs)   │  │  (Tenant,    │  │  Jobs         │ │
│  │              │  │   Auth, Rate │  │  (Hangfire)   │ │
│  └──────┬───────┘  │   Limit)     │  └───────────────┘ │
│         │          └──────────────┘                     │
│  ┌──────▼──────────────────────────────────────────┐   │
│  │         Services (48 Business Logic)            │   │
│  │    Employee • Attendance • Leave • Payroll      │   │
│  └─────────────────────┬───────────────────────────┘   │
└────────────────────────┼─────────────────────────────────┘
                         │
         ┌───────────────┼───────────────┐
         │               │               │
┌────────▼────────┐ ┌───▼──────┐ ┌─────▼──────┐
│  PostgreSQL 16  │ │  Redis   │ │ ZKTeco     │
│  (Cloud SQL)    │ │  Cache   │ │ Biometric  │
│                 │ │          │ │ Devices    │
│ • master        │ └──────────┘ └────────────┘
│ • tenant_1      │
│ • tenant_2      │
│ • tenant_N      │
└─────────────────┘
```

**Architecture Details:** [docs/architecture/](docs/architecture/)

---

## ✨ Features

### Core HR
- ✅ Employee management with photo uploads
- ✅ Department & organizational hierarchy
- ✅ Leave management (annual, sick, unpaid)
- ✅ Attendance tracking (biometric integration)
- ✅ Timesheet management
- ✅ Payroll processing (Mauritius compliant)

### Biometric Integration
- ✅ ZKTeco device integration
- ✅ Real-time punch sync
- ✅ Device management UI
- ✅ Webhook support for auto-sync

### Security & Compliance
- ✅ Multi-factor authentication (TOTP)
- ✅ AES-256-GCM encryption
- ✅ Argon2 password hashing
- ✅ Comprehensive audit logging
- ✅ JWT with refresh tokens
- ✅ Role-based access control

### Multi-Tenancy
- ✅ Schema-per-tenant isolation
- ✅ Subdomain routing (tenant.morishr.com)
- ✅ Tenant-specific branding
- ✅ Subscription management

### Missing Features (See [Issues Doc](COMPREHENSIVE_ISSUES_AND_FIXES_REQUIRED.md))
- ❌ Performance management
- ❌ Recruitment/ATS
- ❌ Benefits management
- ❌ Asset management
- ❌ Travel & expense
- ❌ Exit management

---

## 🛠️ Tech Stack

### Backend
- **.NET 9.0** (C# LTS framework)
- **ASP.NET Core** Web API
- **Entity Framework Core 9.0**
- **PostgreSQL 16** (Cloud SQL)
- **Hangfire** (background jobs)
- **Serilog** (structured logging)
- **Google Cloud** (Secret Manager, Storage, Logging)

### Frontend
- **Angular 20.3** (latest stable)
- **Angular Material** UI components
- **RxJS 7.8** (reactive programming)
- **Chart.js** (data visualization)
- **SignalR** (real-time updates)

### DevOps
- ⚠️ **Docker** (missing for main API)
- ⚠️ **CI/CD** (no GitHub Actions)
- ✅ **Cloud Run** ready (needs cloudbuild.yaml)
- ✅ **Redis** (caching - underutilized)

---

## 📊 System Status

| Component | Status | Coverage | Issues |
|-----------|--------|----------|--------|
| Backend API | ✅ Working | 505 files | 5 critical bugs |
| Frontend | ✅ Fixed | 56 components | Bundle size warning |
| Database | ✅ Working | 36 migrations | Missing indexes |
| Tests | 🔴 **1.2%** | 6 test files | Need 60% coverage |
| Docker | 🔴 Missing | - | P1 priority |
| CI/CD | 🔴 Missing | - | P1 priority |
| Docs | ✅ Organized | 156 files | Just cleaned up |

**Detailed Status:** [COMPREHENSIVE_ISSUES_AND_FIXES_REQUIRED.md](COMPREHENSIVE_ISSUES_AND_FIXES_REQUIRED.md)

---

## 🚦 Getting Started

### Prerequisites
- .NET 9.0 SDK
- Node.js 20+ & npm
- PostgreSQL 16
- Redis (optional, for caching)
- Google Cloud account (for production)

### Local Development
```bash
# 1. Clone repository
git clone https://github.com/MorisHR/HRAPP.git
cd HRAPP

# 2. Set up database
psql -U postgres -c "CREATE DATABASE hrms_master;"
cd src/HRMS.API
dotnet ef database update

# 3. Run backend
dotnet run

# 4. Run frontend (new terminal)
cd hrms-frontend
npm install
npm start
```

**📖 Detailed Guide:** [docs/guides/SYSTEM_SETUP_GUIDE.md](docs/guides/SYSTEM_SETUP_GUIDE.md)

---

## 🔐 Security

### Authentication
- JWT tokens with refresh token rotation
- Argon2 password hashing
- MFA via TOTP (Google Authenticator)
- Session timeout warnings

### Data Protection
- AES-256-GCM encryption for sensitive fields
- Schema-per-tenant isolation
- Audit logging with tamper detection
- GDPR-compliant data retention

**Security Docs:** [docs/security/](docs/security/)

---

## 📈 Performance

### Current Capacity
- **Tested:** 10 tenants, 100 employees each
- **Target:** 100 tenants, 1,000 employees each
- **Bottlenecks:** Connection pool (500 → need 1,500), DbContext creation

### Optimization Needed
- ⚠️ Redis caching underutilized (only 3 services)
- ⚠️ Missing database indexes
- ⚠️ No query optimization audit
- ⚠️ Bundle size: 666 KB (target: <500 KB)

**Performance Guide:** [docs/guides/PERFORMANCE_QUICK_REFERENCE.md](docs/guides/PERFORMANCE_QUICK_REFERENCE.md)

---

## 🧪 Testing

```bash
# Backend tests (minimal - 1.2% coverage)
cd tests/HRMS.Tests
dotnet test

# Frontend tests
cd hrms-frontend
npm test
```

**⚠️ Critical Gap:** Only 6 test files for 505 C# files. Need 60% coverage before production.

**Testing Docs:** [docs/testing/](docs/testing/)

---

## 📦 Deployment

### Production Checklist
- [ ] Fix 5 critical P0 bugs
- [ ] Create Docker configuration
- [ ] Set up CI/CD pipeline
- [ ] Add database indexes
- [ ] Achieve 60% test coverage
- [ ] Configure backups
- [ ] Set up monitoring alerts
- [ ] Migrate secrets to Secret Manager

**Deployment Guide:** [docs/deployment/DEPLOYMENT_GUIDE.md](docs/deployment/DEPLOYMENT_GUIDE.md)

---

## 🐛 Known Issues

**See [COMPREHENSIVE_ISSUES_AND_FIXES_REQUIRED.md](COMPREHENSIVE_ISSUES_AND_FIXES_REQUIRED.md) for all 65 issues.**

### Top Priority
1. DateTime precision causing audit checksum failures (production bug)
2. DbContext creation pattern causing 5-15ms overhead per request
3. TenantService race condition (security risk)
4. Connection pool exhaustion for 100 tenants
5. ThreadPool exhaustion via Task.Run

---

## 📞 Support

### Documentation
- [Master Index](docs/README.md) - All documentation
- [Quick Reference](docs/guides/QUICK_REFERENCE.md) - Common operations
- [Troubleshooting](COMPREHENSIVE_ISSUES_AND_FIXES_REQUIRED.md) - Known issues

### Repository
- **GitHub:** https://github.com/MorisHR/HRAPP
- **Current Branch:** `claude/engineering-guidelines-01AP2YmAF8FCyYCKxbHyjsGu`

---

## 📝 Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history and changes.

---

## 📄 License

**Proprietary** - All rights reserved. This is a private commercial project.

---

## 👥 Contributors

- **Development Team:** MorisHR Engineering
- **Last Major Update:** 2025-11-19 (Documentation cleanup + bug identification)

---

**🚀 Ready to fix those critical bugs and ship to production!**
