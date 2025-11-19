# HRMS Documentation

This directory contains organized documentation for the HRMS (Human Resources Management System) project.

## Directory Structure

### 📐 Architecture (12 docs)
System architecture, design decisions, and technical design documents.
- Design system documentation
- Frontend services architecture
- Subscription flow architecture
- Error handling system
- Performance analysis

### 🚀 Deployment (17 docs)
Production deployment guides, checklists, and operational procedures.
- Deployment guide and checklists
- Database migration runbook
- Database rollback procedures
- Monitoring and alerting strategies
- Quick start guides
- System startup procedures

### 💻 Development (13 docs)
Developer guides, development workflows, and technical references.
- CI/CD pipeline specifications
- Pull request quality gates
- Custom component library guide
- Database development guides
- Quick reference materials

### ⭐ Features (19 docs)
Feature-specific documentation and implementation guides.
- MFA (Multi-Factor Authentication) implementation
- Audit logging system
- Hangfire background jobs
- Industry sectors and geographic locations
- Subscription management
- Tenant activation
- Timesheet module
- ZKTeco biometric device integration

### 🔄 Migration (9 docs)
Migration guides and strategies for upgrades and transitions.
- Angular 20 migration
- Database migration procedures
- Tenant migration automation
- Dual-run migration strategy
- Visual migration comparison

### 🔒 Security (6 docs)
Security policies, audit reports, and hardening recommendations.
- Security audit reports
- Security hardening recommendations
- Encryption implementation
- Security headers configuration
- Multi-tenant security

### 🧪 Testing (10 docs)
Testing strategies, test reports, and QA documentation.
- Automated testing strategy
- QA infrastructure guide
- Test reports and validation
- Subscription testing checklist

### 📦 Archived (79 docs)
Historical session reports and implementation summaries.
- Phase/Wave completion reports
- Fortune 500 implementation sessions
- Executive summaries
- Session completion reports

## Root Documentation

Essential project documentation kept in the repository root:

- **README.md** - Project overview and getting started guide
- **CHANGELOG.md** - Version history and release notes
- **SECURITY.md** - Security policies and vulnerability reporting
- **CLAUDE_CONTEXT.md** - AI assistant context and project knowledge

## Documentation Standards

### File Naming
- Use descriptive, UPPERCASE_SNAKE_CASE for documentation files
- Include category prefix where appropriate (e.g., `SECURITY_`, `TESTING_`)
- Use `.md` extension for all markdown files

### Document Structure
Each document should include:
- Title and brief description
- Table of contents (for longer docs)
- Clear sections with headers
- Code examples where applicable
- Last updated date

### Archiving Policy
- Session reports and completion summaries are archived after project milestones
- Duplicate or superseded documentation is removed
- Active development docs remain in category folders
- Archived docs preserved for historical reference only

## Quick Navigation

**New to the project?** Start with:
1. `/README.md` - Project overview
2. `deployment/QUICK_START_OPERATIONS_GUIDE.md` - Get the system running
3. `development/CUSTOM_COMPONENT_LIBRARY_GUIDE.md` - Understand the codebase
4. `features/` - Explore implemented features

**Deploying to production?** See:
1. `deployment/DEPLOYMENT_GUIDE.md`
2. `deployment/DEPLOYMENT_CHECKLIST.md`
3. `deployment/SYSTEM_STARTUP_CHECKLIST.md`
4. `security/SECURITY_HARDENING_RECOMMENDATIONS.md`

**Developing features?** Check:
1. `development/CI_CD_QUALITY_GATES.md`
2. `development/PULL_REQUEST_QUALITY_GATES.md`
3. `testing/AUTOMATED_TESTING_STRATEGY.md`
4. `architecture/` - Understand system design

---

*Last Updated: November 19, 2025*
*Total Documents: 189 (excludes node_modules)*
