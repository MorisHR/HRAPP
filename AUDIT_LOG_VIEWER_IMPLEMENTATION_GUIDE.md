# Audit Log Viewer Implementation Guide

## ✅ COMPLETED: Backend Implementation

### 1. Controllers Created

#### SuperAdmin Audit Log Controller
**File:** `src/HRMS.API/Controllers/AuditLogController.cs`
- **Route:** `/api/superadmin/auditlog`
- **Authorization:** SuperAdmin role only
- **Scope:** System-wide (ALL tenants)

**Endpoints:**
- `GET /api/superadmin/auditlog` - Get paginated audit logs
- `GET /api/superadmin/auditlog/{id}` - Get audit log details
- `GET /api/superadmin/auditlog/statistics` - Get system statistics
- `POST /api/superadmin/auditlog/export` - Export to CSV
- `GET /api/superadmin/auditlog/failed-logins` - Security monitoring
- `GET /api/superadmin/auditlog/critical-events` - Critical events

#### Tenant Admin Audit Log Controller
**File:** `src/HRMS.API/Controllers/TenantAuditLogController.cs`
- **Route:** `/api/tenant/auditlog`
- **Authorization:** Tenant Admin + TenantAuthorizationFilter
- **Scope:** Tenant-specific ONLY

**Endpoints:**
- `GET /api/tenant/auditlog` - Get tenant audit logs (TenantId enforced)
- `GET /api/tenant/auditlog/{id}` - Get audit log (validates tenant ownership)
- `GET /api/tenant/auditlog/statistics` - Get tenant statistics
- `POST /api/tenant/auditlog/export` - Export tenant logs
- `GET /api/tenant/auditlog/failed-logins` - Tenant failed logins
- `GET /api/tenant/auditlog/sensitive-changes` - Sensitive data changes
- `GET /api/tenant/auditlog/user-activity` - User activity report

### 2. DTOs Created

**File:** `src/HRMS.Application/DTOs/AuditLog/AuditLogFilterDto.cs`
- Comprehensive filter criteria
- Pagination support
- Sort configuration

**File:** `src/HRMS.Application/DTOs/AuditLog/AuditLogDto.cs`
- `AuditLogDto` - List view
- `AuditLogDetailDto` - Detail view with OldValues/NewValues
- `PagedResult<T>` - Pagination wrapper
- `AuditLogStatisticsDto` - Dashboard statistics
- `UserActivityDto` - User activity summaries

### 3. Security Features

**Tenant Isolation (CRITICAL):**
```csharp
// Tenant controller ALWAYS enforces TenantId
filter.TenantId = tenantId.Value;  // Never trust frontend

// Detail view validates ownership
if (log.TenantId != tenantId.Value)
{
    return Forbid();  // Cannot access other tenant's logs
}
```

**Authorization:**
- SuperAdmin: `[Authorize(Roles = "SuperAdmin")]`
- Tenant: `[Authorize]` + `[ServiceFilter(typeof(TenantAuthorizationFilter))]`

---

## 📋 TODO: Frontend Implementation

### Phase 1: Angular Service

Create `src/hrms-frontend/src/app/services/audit-log.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export interface AuditLogFilter {
  tenantId?: string;
  startDate?: Date;
  endDate?: Date;
  userEmail?: string;
  actionTypes?: number[];
  categories?: number[];
  severities?: number[];
  entityType?: string;
  pageNumber: number;
  pageSize: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AuditLogService {
  private superAdminApiUrl = `${environment.apiUrl}/superadmin/auditlog`;
  private tenantApiUrl = `${environment.apiUrl}/tenant/auditlog`;

  constructor(private http: HttpClient) {}

  // SuperAdmin methods (system-wide)
  getSystemAuditLogs(filter: AuditLogFilter): Observable<any> {
    let params = this.buildHttpParams(filter);
    return this.http.get(`${this.superAdminApiUrl}`, { params });
  }

  getSystemAuditLogById(id: string): Observable<any> {
    return this.http.get(`${this.superAdminApiUrl}/${id}`);
  }

  getSystemStatistics(startDate?: Date, endDate?: Date): Observable<any> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate.toISOString());
    if (endDate) params = params.set('endDate', endDate.toISOString());
    return this.http.get(`${this.superAdminApiUrl}/statistics`, { params });
  }

  exportSystemLogs(filter: AuditLogFilter): Observable<Blob> {
    return this.http.post(`${this.superAdminApiUrl}/export`, filter, {
      responseType: 'blob'
    });
  }

  // Tenant methods (tenant-scoped)
  getTenantAuditLogs(filter: AuditLogFilter): Observable<any> {
    let params = this.buildHttpParams(filter);
    return this.http.get(`${this.tenantApiUrl}`, { params });
  }

  getTenantAuditLogById(id: string): Observable<any> {
    return this.http.get(`${this.tenantApiUrl}/${id}`);
  }

  getTenantStatistics(startDate?: Date, endDate?: Date): Observable<any> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate.toISOString());
    if (endDate) params = params.set('endDate', endDate.toISOString());
    return this.http.get(`${this.tenantApiUrl}/statistics`, { params });
  }

  exportTenantLogs(filter: AuditLogFilter): Observable<Blob> {
    return this.http.post(`${this.tenantApiUrl}/export`, filter, {
      responseType: 'blob'
    });
  }

  getTenantFailedLogins(startDate?: Date, endDate?: Date): Observable<any> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate.toISOString());
    if (endDate) params = params.set('endDate', endDate.toISOString());
    return this.http.get(`${this.tenantApiUrl}/failed-logins`, { params });
  }

  getSensitiveChanges(startDate?: Date, endDate?: Date): Observable<any> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate.toISOString());
    if (endDate) params = params.set('endDate', endDate.toISOString());
    return this.http.get(`${this.tenantApiUrl}/sensitive-changes`, { params });
  }

  private buildHttpParams(filter: AuditLogFilter): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber.toString())
      .set('pageSize', filter.pageSize.toString());

    if (filter.startDate) params = params.set('startDate', filter.startDate.toISOString());
    if (filter.endDate) params = params.set('endDate', filter.endDate.toISOString());
    if (filter.userEmail) params = params.set('userEmail', filter.userEmail);
    if (filter.entityType) params = params.set('entityType', filter.entityType);
    if (filter.sortBy) params = params.set('sortBy', filter.sortBy);
    if (filter.sortDescending !== undefined) params = params.set('sortDescending', filter.sortDescending.toString());

    // Array parameters
    if (filter.actionTypes?.length) {
      filter.actionTypes.forEach(at => params = params.append('actionTypes', at.toString()));
    }
    if (filter.categories?.length) {
      filter.categories.forEach(c => params = params.append('categories', c.toString()));
    }
    if (filter.severities?.length) {
      filter.severities.forEach(s => params = params.append('severities', s.toString()));
    }

    return params;
  }
}
```

### Phase 2: Shared Components

#### Component Structure
```
src/hrms-frontend/src/app/features/audit-logs/
├── components/
│   ├── audit-log-table/
│   │   ├── audit-log-table.component.ts
│   │   ├── audit-log-table.component.html
│   │   └── audit-log-table.component.scss
│   ├── audit-log-filters/
│   │   ├── audit-log-filters.component.ts
│   │   ├── audit-log-filters.component.html
│   │   └── audit-log-filters.component.scss
│   ├── audit-log-detail-modal/
│   │   ├── audit-log-detail-modal.component.ts
│   │   ├── audit-log-detail-modal.component.html
│   │   └── audit-log-detail-modal.component.scss
│   └── audit-log-statistics/
│       ├── audit-log-statistics.component.ts
│       ├── audit-log-statistics.component.html
│       └── audit-log-statistics.component.scss
├── superadmin/
│   └── superadmin-audit-logs.component.ts
├── tenant/
│   └── tenant-audit-logs.component.ts
└── audit-logs.module.ts
```

#### Key Component Features

**Audit Log Table Component:**
- Displays paginated audit logs
- Sortable columns
- Row actions (view details)
- Color-coded badges for severity/category
- Responsive design

**Filter Panel Component:**
- Date range picker (Material DatePicker)
- Multi-select dropdowns
- Search input
- Apply/Clear buttons
- Filter chips showing active filters

**Detail Modal Component:**
- Full audit log information
- JSON syntax highlighting for OldValues/NewValues
- Changed fields highlighted
- Metadata display
- Print/Export button

**Statistics Dashboard:**
- Cards showing key metrics
- Charts (consider ngx-charts or Chart.js)
- Date range selector
- Quick links to filtered views

### Phase 3: Page Components

#### SuperAdmin Audit Logs Page

**File:** `src/hrms-frontend/src/app/features/admin/audit-logs/superadmin-audit-logs.component.ts`

```typescript
import { Component, OnInit } from '@angular/core';
import { AuditLogService, AuditLogFilter } from '../../../services/audit-log.service';

@Component({
  selector: 'app-superadmin-audit-logs',
  templateUrl: './superadmin-audit-logs.component.html',
  styleUrls: ['./superadmin-audit-logs.component.scss']
})
export class SuperadminAuditLogsComponent implements OnInit {
  auditLogs: any[] = [];
  loading = false;
  totalCount = 0;

  filter: AuditLogFilter = {
    pageNumber: 1,
    pageSize: 50
  };

  constructor(private auditLogService: AuditLogService) {}

  ngOnInit() {
    this.loadAuditLogs();
  }

  loadAuditLogs() {
    this.loading = true;
    this.auditLogService.getSystemAuditLogs(this.filter).subscribe({
      next: (response) => {
        this.auditLogs = response.data.items;
        this.totalCount = response.data.totalCount;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading audit logs', error);
        this.loading = false;
      }
    });
  }

  onFilterChange(newFilter: AuditLogFilter) {
    this.filter = { ...this.filter, ...newFilter, pageNumber: 1 };
    this.loadAuditLogs();
  }

  onPageChange(pageNumber: number) {
    this.filter.pageNumber = pageNumber;
    this.loadAuditLogs();
  }

  exportLogs() {
    this.auditLogService.exportSystemLogs(this.filter).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `audit_logs_${new Date().getTime()}.csv`;
        link.click();
      },
      error: (error) => console.error('Export failed', error)
    });
  }
}
```

#### Tenant Admin Audit Logs Page

Similar structure but calls `getTenantAuditLogs()` instead.

**Key Difference:** No tenant filter selector in UI (backend enforces automatically).

### Phase 4: Routing

**SuperAdmin Routes** (`src/hrms-frontend/src/app/features/admin/admin-routing.module.ts`):
```typescript
{
  path: 'audit-logs',
  component: SuperadminAuditLogsComponent,
  canActivate: [AuthGuard],
  data: { roles: ['SuperAdmin'] }
}
```

**Tenant Routes** (`src/hrms-frontend/src/app/features/tenant/tenant-routing.module.ts`):
```typescript
{
  path: 'audit-logs',
  component: TenantAuditLogsComponent,
  canActivate: [AuthGuard, TenantGuard]
}
```

### Phase 5: Navigation Links

**SuperAdmin Sidebar:**
```html
<a routerLink="/admin/audit-logs" routerLinkActive="active">
  <mat-icon>history</mat-icon>
  <span>Audit Logs</span>
</a>
```

**Tenant Sidebar:**
```html
<a routerLink="/tenant/audit-logs" routerLinkActive="active">
  <mat-icon>security</mat-icon>
  <span>Audit Trail</span>
</a>
```

---

## 🎨 Design Guidelines

### Color Coding

**Severity Badges:**
```scss
.severity-badge {
  &.info { background: #3b82f6; color: white; }
  &.warning { background: #f59e0b; color: white; }
  &.critical { background: #ef4444; color: white; }
  &.emergency { background: #7f1d1d; color: white; }
}
```

**Category Badges:**
```scss
.category-badge {
  &.authentication { background: #8b5cf6; }
  &.authorization { background: #6366f1; }
  &.data-change { background: #3b82f6; }
  &.tenant-lifecycle { background: #10b981; }
  &.security-event { background: #ef4444; }
  &.compliance { background: #14b8a6; }
}
```

### Table Design

Match existing MorisHR table styling:
- Executive theme (Fortune 500 style)
- Clean typography
- Hover effects
- Sticky header
- Responsive card view on mobile

### Empty States

```html
<div class="empty-state" *ngIf="!loading && auditLogs.length === 0">
  <mat-icon>history</mat-icon>
  <h3>No Audit Logs Found</h3>
  <p>Try adjusting your filters or date range</p>
</div>
```

---

## 🔒 Security Checklist

- ✅ Backend enforces TenantId filtering (never trust frontend)
- ✅ SuperAdmin authorization on system-wide endpoints
- ✅ Tenant authorization + ownership validation
- ✅ Detail view validates tenant ownership
- ✅ Export limited by role
- ⏳ Frontend route guards (TODO)
- ⏳ Frontend role-based UI hiding (TODO)

---

## 🧪 Testing Requirements

### Backend Tests
- [ ] Test SuperAdmin can see all tenant logs
- [ ] Test Tenant Admin can only see own logs
- [ ] Test Tenant Admin cannot access other tenant logs (403)
- [ ] Test detail view ownership validation
- [ ] Test export functionality
- [ ] Test pagination
- [ ] Test filtering

### Frontend Tests
- [ ] Test SuperAdmin page loads
- [ ] Test Tenant page loads
- [ ] Test filter application
- [ ] Test pagination
- [ ] Test export download
- [ ] Test responsive design
- [ ] Test error states
- [ ] Test loading states

---

## 📊 Performance Targets

- Page load: < 2 seconds
- Filter application: < 500ms
- Pagination: < 300ms
- Export (1000 records): < 5 seconds
- Smooth with 10,000+ records (pagination)

---

## 🚀 Next Steps

1. **Complete Angular Service** (Phase 1)
2. **Create Shared Components** (Phase 2)
   - Start with table component
   - Then filter panel
   - Then detail modal
3. **Create Page Components** (Phase 3)
   - SuperAdmin page first
   - Then Tenant page (reuse components)
4. **Add Routing** (Phase 4)
5. **Test Everything** (Phase 5)

---

## 📝 Notes

**Backend is 100% ready!** Controllers, DTOs, and security are fully implemented.

**Frontend needs:**
- Angular service (1-2 hours)
- Shared components (4-6 hours)
- Page integration (2-3 hours)
- Styling to match existing theme (2-3 hours)
- Testing (2-3 hours)

**Total estimate: 1-2 days for complete frontend implementation**

**Key Success Factor:** Tenant isolation is ENFORCED BY BACKEND. The frontend just needs to call the right endpoint - the backend guarantees data security.
