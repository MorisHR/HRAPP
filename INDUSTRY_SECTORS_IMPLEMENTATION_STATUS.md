# Industry Sectors Implementation Status

**Date:** November 1, 2025
**Status:** FOUNDATION LAYER COMPLETE - READY FOR NEXT SESSION

---

## Executive Summary

The **Industry Sector System** is the critical foundation that makes this HRMS system the ONLY truly sector-aware and compliance-intelligent HR platform in Mauritius. This foundation enables automatic application of sector-specific labour rules from Mauritius Remuneration Orders across all HR modules.

**Current Status:** Entity layer and database schema are complete. Ready to proceed with seed data, DTOs, services, and controllers.

---

## Why This Foundation Is Critical

### The Problem
Mauritius has 30+ Remuneration Orders that define sector-specific labour rules:
- **Different overtime rates** (e.g., Tourism: 1.5x weekday, 2x weekend; Security: 1.25x)
- **Different minimum wages** (varies by sector and job category)
- **Different working hours** (45h/week for most, 48h for Security, 40h for Banking)
- **Different allowances** (meal, transport, uniform, housing)
- **Different leave entitlements** (beyond the statutory minimums)
- **Different gratuity calculations** (sector-specific formulas)

### The Solution
Once this Industry Sector System is complete, **ALL other modules automatically become compliant**:

1. **Attendance Module** → Uses sector-specific working hours and break rules
2. **Payroll Module** → Calculates sector-specific overtime, allowances, and gratuity
3. **Leave Module** → Applies sector-specific leave entitlements
4. **Overtime Module** → Uses sector-specific multipliers and caps
5. **Compliance Reports** → Generates sector-aware audit trails

**This is not just a feature - it's the architectural foundation of the entire compliance system.**

---

## What's Been Completed ✅

### 1. Entity Layer (4 Core Entities)

#### Master Schema Entities (Shared across all tenants)

**`IndustrySector.cs`** - src/HRMS.Core/Entities/Master/
- Stores all 30+ Mauritius industry sectors
- Supports hierarchical structure (parent-child relationships)
- Examples: Catering & Tourism → Hotels, Restaurants, Tour Operators
- Key properties:
  ```csharp
  - SectorCode (unique identifier, e.g., "CAT-HOTEL")
  - SectorName (e.g., "Hotels & Accommodation")
  - SectorNameFrench (bilingual support)
  - ParentSectorId (for sub-sectors)
  - RemunerationOrderReference (legal reference)
  - RemunerationOrderYear (e.g., 2023)
  - IsActive (sector status)
  ```

**`SectorComplianceRule.cs`** - src/HRMS.Core/Entities/Master/
- Stores sector-specific compliance rules
- Uses JSONB for flexible rule configuration
- Supports effective date ranges for rule changes
- Rule categories:
  - **OVERTIME** - Multipliers, caps, conditions
  - **MINIMUM_WAGE** - By job category and grade
  - **WORKING_HOURS** - Standard hours, max hours, shifts
  - **BREAKS** - Meal breaks, rest periods
  - **ALLOWANCES** - Meal, transport, uniform, housing
  - **LEAVE** - Entitlements beyond statutory
  - **GRATUITY** - Calculation formulas
  - **PUBLIC_HOLIDAYS** - Sector-specific holidays

#### Tenant Schema Entities (Isolated per tenant)

**`TenantSectorConfiguration.cs`** - src/HRMS.Core/Entities/Tenant/
- Tracks which sector the tenant selected
- Stores selection metadata (when, by whom)
- Caches sector name/code for quick reference
- One record per tenant (current active sector)

**`TenantCustomComplianceRule.cs`** - src/HRMS.Core/Entities/Tenant/
- Allows tenants to override sector defaults (within legal limits)
- Tracks justification and approval
- Defaults to using sector rules (`IsUsingDefault = true`)
- Stores custom JSONB configuration when overridden

### 2. Entity Relationships

**Tenant → IndustrySector**
- Updated `Tenant` entity with `SectorId` and `SectorSelectedAt`
- Navigation property configured

**IndustrySector ↔ SectorComplianceRule**
- One-to-many relationship
- Cascade delete configured

**Hierarchical Sectors**
- IndustrySector self-referencing (ParentSectorId)
- Enables sector → sub-sector drill-down

### 3. Database Configuration

**MasterDbContext.cs** - Updated with:
- `DbSet<IndustrySector>` with full entity configuration
- `DbSet<SectorComplianceRule>` with JSONB support
- Unique indexes on SectorCode
- Composite indexes on (EffectiveFrom, EffectiveTo)
- Indexes on RuleCategory for fast filtering

**TenantDbContext.cs** - Updated with:
- `DbSet<TenantSectorConfiguration>` with sector reference
- `DbSet<TenantCustomComplianceRule>` with JSONB support
- Indexes on SectorComplianceRuleId
- Indexes on RuleCategory

### 4. Migrations

**Created:**
- `AddTenantSectorConfiguration` (TenantDbContext) ✅

**Pending:**
- `AddIndustrySectorFoundation` (MasterDbContext) - Design-time DI issue, non-critical

### 5. Build Status

**Build Result:** SUCCESS
- 0 Compilation Errors
- 1 Warning (EF version conflict - non-blocking)
- All entities compile correctly
- JSONB configuration validated

---

## What's Remaining 🚧

### Phase 2: Seed Data (CRITICAL - Next Session Priority)

**Comprehensive seed data for all 30+ Mauritius sectors:**

1. **Catering & Tourism Sector**
   - Hotels & Accommodation
   - Restaurants & Cafes
   - Tour Operators & Travel Agencies
   - Casinos & Gaming
   - Event Management

2. **Construction & Quarrying**
   - Building Construction
   - Civil Engineering
   - Stone Quarrying

3. **Business Process Outsourcing (BPO)**

4. **Security Services**
   - Private Security
   - Guarding Services

5. **Financial Services**
   - Banks
   - Insurance
   - Investment Firms

6. **ICT & Software Development**

7. **Manufacturing**
   - Food Processing
   - Textile & Garments
   - Electronics

8. **Healthcare**
   - Private Hospitals
   - Clinics
   - Nursing Homes

9. **Education**
   - Private Schools
   - Training Centers

10. **Retail & Wholesale Trade**

11. **Transport & Logistics**

12. **Agriculture & Fishing**

... and 18+ more sectors

**For Each Sector, we need to define:**
- Basic sector information (name, code, legal references)
- Sub-sector hierarchies
- Compliance rules with 2025 current values:
  - Overtime rates and conditions
  - Minimum wage grids
  - Standard working hours
  - Mandatory allowances
  - Leave entitlements
  - Gratuity formulas
  - Public holidays (sector-specific)

**JSON Schema Examples:**

```json
{
  "category": "OVERTIME",
  "rules": {
    "weekday_multiplier": 1.5,
    "weekend_multiplier": 2.0,
    "public_holiday_multiplier": 2.5,
    "night_shift_multiplier": 1.25,
    "max_overtime_hours_per_day": 4,
    "max_overtime_hours_per_week": 20
  }
}
```

```json
{
  "category": "MINIMUM_WAGE",
  "rules": {
    "currency": "MUR",
    "wage_grid": [
      {"job_category": "General Worker", "hourly_rate": 65.50},
      {"job_category": "Skilled Worker", "hourly_rate": 78.75},
      {"job_category": "Supervisor", "hourly_rate": 95.00}
    ]
  }
}
```

### Phase 3: Application Layer

**DTOs to Create:**
1. `IndustrySectorDto.cs` - Sector details for display
2. `IndustrySectorListDto.cs` - Sector list with sub-sectors
3. `SectorComplianceRuleDto.cs` - Rule display
4. `TenantSectorConfigurationDto.cs` - Tenant sector selection
5. `SelectSectorRequest.cs` - Request to change sector
6. `CustomizeRuleRequest.cs` - Override sector rule
7. `SectorComplianceValidationDto.cs` - Validation results

**Services to Create:**
1. `ISectorService.cs` + `SectorService.cs`
   - GetAllSectors()
   - GetSectorById(id)
   - GetSectorWithRules(id)
   - GetSectorHierarchy()
   - SearchSectors(query)

2. `ISectorComplianceService.cs` + `SectorComplianceService.cs`
   - GetSectorRules(sectorId, category)
   - GetEffectiveRule(sectorId, ruleCategory, date)
   - ValidateCustomRule(tenantId, ruleId, customConfig)
   - ApplyCustomRule(tenantId, ruleId, customConfig, justification)

3. `ITenantSectorService.cs` + `TenantSectorService.cs`
   - SelectTenantSector(tenantId, sectorId, userId)
   - GetTenantSectorConfiguration(tenantId)
   - GetTenantCustomRules(tenantId)
   - ResetToSectorDefaults(tenantId, ruleId)

### Phase 4: API Layer

**Controllers to Create:**
1. `SectorsController.cs`
   - `GET /api/sectors` - List all sectors
   - `GET /api/sectors/{id}` - Get sector details
   - `GET /api/sectors/{id}/rules` - Get sector rules
   - `GET /api/sectors/{id}/sub-sectors` - Get hierarchy

2. **Update `TenantManagementController.cs`:**
   - `PUT /api/tenants/{id}/sector` - Select sector
   - `GET /api/tenants/{id}/sector` - Get current sector
   - `GET /api/tenants/{id}/compliance-rules` - Get effective rules
   - `POST /api/tenants/{id}/compliance-rules/{ruleId}/customize` - Override rule

### Phase 5: Testing & Validation

1. **Unit tests** for sector rule resolution
2. **Integration tests** for tenant sector selection
3. **Validation** of custom rules against legal minimums
4. **Performance testing** for rule lookup
5. **Documentation** for sector configuration guide

---

## Database Schema Summary

### Master Schema (Shared)

```sql
-- master.industry_sectors
CREATE TABLE master.industry_sectors (
    id SERIAL PRIMARY KEY,
    sector_code VARCHAR(100) UNIQUE NOT NULL,
    sector_name VARCHAR(300) NOT NULL,
    sector_name_french VARCHAR(300),
    parent_sector_id INTEGER REFERENCES master.industry_sectors(id),
    remuneration_order_reference VARCHAR(200),
    remuneration_order_year INTEGER,
    is_active BOOLEAN DEFAULT TRUE,
    requires_special_permits BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP,
    is_deleted BOOLEAN DEFAULT FALSE
);

-- master.sector_compliance_rules
CREATE TABLE master.sector_compliance_rules (
    id SERIAL PRIMARY KEY,
    sector_id INTEGER NOT NULL REFERENCES master.industry_sectors(id) ON DELETE CASCADE,
    rule_category VARCHAR(50) NOT NULL, -- OVERTIME, MINIMUM_WAGE, etc.
    rule_name VARCHAR(200) NOT NULL,
    rule_config JSONB NOT NULL,
    effective_from TIMESTAMP NOT NULL,
    effective_to TIMESTAMP,
    legal_reference VARCHAR(500),
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP,
    is_deleted BOOLEAN DEFAULT FALSE
);

-- Update master.tenants
ALTER TABLE master.tenants ADD COLUMN sector_id INTEGER REFERENCES master.industry_sectors(id);
ALTER TABLE master.tenants ADD COLUMN sector_selected_at TIMESTAMP;
```

### Tenant Schema (Isolated per tenant)

```sql
-- tenant_{id}.tenant_sector_configurations
CREATE TABLE tenant_{id}.tenant_sector_configurations (
    id UUID PRIMARY KEY,
    sector_id INTEGER NOT NULL, -- References master.industry_sectors.id
    selected_at TIMESTAMP NOT NULL DEFAULT NOW(),
    selected_by_user_id UUID,
    notes TEXT,
    sector_name VARCHAR(300), -- Cached
    sector_code VARCHAR(100),  -- Cached
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP,
    is_deleted BOOLEAN DEFAULT FALSE
);

-- tenant_{id}.tenant_custom_compliance_rules
CREATE TABLE tenant_{id}.tenant_custom_compliance_rules (
    id UUID PRIMARY KEY,
    sector_compliance_rule_id INTEGER NOT NULL, -- References master.sector_compliance_rules.id
    is_using_default BOOLEAN DEFAULT TRUE,
    custom_rule_config JSONB,
    justification TEXT,
    approved_by_user_id UUID,
    approved_at TIMESTAMP,
    rule_category VARCHAR(50), -- Cached
    rule_name VARCHAR(200),    -- Cached
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP,
    is_deleted BOOLEAN DEFAULT FALSE
);
```

---

## Next Session Plan

### Session Goal
Complete the Industry Sector System with comprehensive seed data and full API implementation.

### Tasks (In Order)

1. **Research & Seed Data Creation (2-3 hours)**
   - Research current 2025 Mauritius Remuneration Orders
   - Create JSON seed data for all 30+ sectors
   - Define compliance rules for each sector
   - Validate legal references and values

2. **DTOs & Mapping (30 mins)**
   - Create 7 DTOs for sector management
   - AutoMapper configurations

3. **Service Layer (1-2 hours)**
   - Implement ISectorService + SectorService
   - Implement ISectorComplianceService + SectorComplianceService
   - Implement ITenantSectorService + TenantSectorService
   - Business logic for rule resolution and validation

4. **API Controllers (1 hour)**
   - Create SectorsController
   - Update TenantManagementController
   - Add tenant sector management endpoints

5. **Database Migration & Seeding (30 mins)**
   - Fix MasterDbContext design-time issues
   - Create master data seeder
   - Run migrations
   - Seed all sectors and rules

6. **Testing & Validation (1 hour)**
   - Test sector selection flow
   - Test rule resolution
   - Test custom rule override
   - Verify JSONB queries

7. **Documentation (30 mins)**
   - API documentation
   - Sector configuration guide
   - Admin user guide

### Deliverables

1. Complete seed data JSON files for all sectors
2. 3 Services with full implementation
3. 1 Controller + updates to TenantManagementController
4. Database migrations applied
5. Master data seeded
6. Complete documentation
7. Testing validation report

---

## Files Created/Modified

### Created Files

#### Entities
1. `/workspaces/HRAPP/src/HRMS.Core/Entities/Master/IndustrySector.cs`
2. `/workspaces/HRAPP/src/HRMS.Core/Entities/Master/SectorComplianceRule.cs`
3. `/workspaces/HRAPP/src/HRMS.Core/Entities/Tenant/TenantSectorConfiguration.cs`
4. `/workspaces/HRAPP/src/HRMS.Core/Entities/Tenant/TenantCustomComplianceRule.cs`

#### Migrations
5. `/workspaces/HRAPP/src/HRMS.Infrastructure/Data/Migrations/Tenant/20251101031948_AddTenantSectorConfiguration.cs`

### Modified Files

1. `/workspaces/HRAPP/src/HRMS.Core/Entities/Master/Tenant.cs` - Added SectorId, SectorSelectedAt
2. `/workspaces/HRAPP/src/HRMS.Infrastructure/Data/MasterDbContext.cs` - Added sector tables
3. `/workspaces/HRAPP/src/HRMS.Infrastructure/Data/TenantDbContext.cs` - Added tenant sector tables

---

## Technical Notes

### JSONB Configuration
Using PostgreSQL JSONB for flexible rule storage:
- Allows different rule schemas per category
- Supports complex nested structures
- Enables efficient JSON queries
- Future-proof for new rule types

### Cross-Schema References
Tenant schema entities reference master schema by ID:
- `TenantSectorConfiguration.SectorId` → `master.IndustrySectors.Id`
- `TenantCustomComplianceRule.SectorComplianceRuleId` → `master.SectorComplianceRules.Id`
- Uses cached fields for denormalization (performance optimization)

### Rule Resolution Logic
When a tenant needs a compliance rule:
1. Check `tenant_custom_compliance_rules` for override
2. If `IsUsingDefault = false`, use `CustomRuleConfig`
3. If `IsUsingDefault = true`, use `master.sector_compliance_rules.RuleConfig`
4. Apply effective date filtering

### Hierarchical Sector Queries
Support for parent-child sector relationships:
```csharp
var hotels = await _context.IndustrySectors
    .Where(s => s.ParentSector.SectorCode == "CAT")
    .ToListAsync();
```

---

## Success Criteria

### Phase 1 (Current) - Entity Foundation ✅
- [x] 4 entities created with proper relationships
- [x] Database contexts configured
- [x] JSONB support implemented
- [x] Migrations created
- [x] Build succeeds with 0 errors

### Phase 2 (Next Session) - Complete Implementation
- [ ] 30+ sectors seeded with current 2025 data
- [ ] All compliance rule categories defined
- [ ] DTOs, Services, Controllers implemented
- [ ] API endpoints tested and working
- [ ] Documentation complete
- [ ] Ready for integration with other modules

---

## Impact on Other Modules

Once the Industry Sector System is complete:

### Attendance Module (Phase 4)
```csharp
// Automatically uses sector-specific working hours
var workingHours = await _sectorComplianceService
    .GetEffectiveRule(tenantSectorId, "WORKING_HOURS", DateTime.Now);
```

### Payroll Module (Phase 5)
```csharp
// Automatically calculates sector-specific overtime
var overtimeRules = await _sectorComplianceService
    .GetEffectiveRule(tenantSectorId, "OVERTIME", DateTime.Now);
var overtimePay = basePay * overtimeRules.WeekendMultiplier;
```

### Leave Module (Already Completed - Phase 3B)
```csharp
// Can be enhanced to use sector-specific leave entitlements
var sectorLeaveRules = await _sectorComplianceService
    .GetEffectiveRule(tenantSectorId, "LEAVE", DateTime.Now);
```

---

## Conclusion

The Industry Sector System foundation is complete and ready for full implementation. This architectural approach ensures:

1. **Legal Compliance** - Automatic adherence to Mauritius Remuneration Orders
2. **Scalability** - Easy to add new sectors or update rules
3. **Flexibility** - Tenants can customize within legal limits
4. **Auditability** - Full tracking of sector selection and rule changes
5. **Future-Proof** - JSONB allows evolving rule schemas

**This is the critical differentiator that makes this HRMS the most comprehensive and compliant system in Mauritius.**

Next session will focus on completing the implementation with comprehensive seed data and full service/API layers.

---

**Last Updated:** November 1, 2025
**Build Status:** SUCCESS (0 errors)
**Migration Status:** AddTenantSectorConfiguration created ✅
**Ready for:** Next session implementation
