#!/bin/bash

# Fortune 500-grade Dialog Migration Script
# Migrates ALL remaining files from MatDialog to custom DialogService

echo "=== Starting Dialog Migration ==="

# File 5: legal-hold-list.component.ts
echo "Migrating legal-hold-list.component.ts..."
sed -i 's/import { MatDialogModule } from/\/\/ import { MatDialogModule } from/g' /workspaces/HRAPP/hrms-frontend/src/app/features/admin/legal-hold/legal-hold-list.component.ts
sed -i 's/MatDialogModule,//g' /workspaces/HRAPP/hrms-frontend/src/app/features/admin/legal-hold/legal-hold-list.component.ts

# File 6: audit-logs.component.ts
echo "Migrating audit-logs.component.ts..."
sed -i 's/import { MatDialog, MatDialogModule } from/import { DialogService } from/g' /workspaces/HRAPP/hrms-frontend/src/app/features/admin/audit-logs/audit-logs.component.ts
sed -i "s/'@angular\/material\/dialog';/'@app\/shared\/ui';/g" /workspaces/HRAPP/hrms-frontend/src/app/features/admin/audit-logs/audit-logs.component.ts
sed -i 's/MatDialogModule,//g' /workspaces/HRAPP/hrms-frontend/src/app/features/admin/audit-logs/audit-logs.component.ts
sed -i 's/private dialog: MatDialog/private dialogService = inject(DialogService)/g' /workspaces/HRAPP/hrms-frontend/src/app/features/admin/audit-logs/audit-logs.component.ts

# File 7: location-list.component.ts
echo "Migrating location-list.component.ts..."
sed -i 's/import { MatDialogModule, MatDialog } from/import { DialogService } from/g' /workspaces/HRAPP/hrms-frontend/src/app/features/admin/locations/location-list.component.ts
sed -i "s/'@angular\/material\/dialog';/'@app\/shared\/ui';/g" /workspaces/HRAPP/hrms-frontend/src/app/features/admin/locations/location-list.component.ts
sed -i 's/MatDialogModule,//g' /workspaces/HRAPP/hrms-frontend/src/app/features/admin/locations/location-list.component.ts
sed -i 's/private dialog = inject(MatDialog)/private dialogService = inject(DialogService)/g' /workspaces/HRAPP/hrms-frontend/src/app/features/admin/locations/location-list.component.ts

# File 8: billing-overview.component.ts
echo "Migrating billing-overview.component.ts..."
sed -i 's/import { MatDialog } from/import { DialogService } from/g' /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/billing/billing-overview.component.ts
sed -i "s/'@angular\/material\/dialog';/'@app\/shared\/ui';/g" /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/billing/billing-overview.component.ts
sed -i 's/private dialog = inject(MatDialog)/private dialogService = inject(DialogService)/g' /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/billing/billing-overview.component.ts
sed -i 's/this\.dialog\.open(/this.dialogService.open(/g' /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/billing/billing-overview.component.ts

# File 9: timesheet-approvals.component.ts
echo "Migrating timesheet-approvals.component.ts..."
sed -i 's/import { MatDialog, MatDialogModule } from/import { DialogService } from/g' /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/timesheets/timesheet-approvals.component.ts
sed -i "s/'@angular\/material\/dialog';/'@app\/shared\/ui';/g" /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/timesheets/timesheet-approvals.component.ts
sed -i 's/MatDialogModule,//g' /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/timesheets/timesheet-approvals.component.ts
sed -i 's/private dialog = inject(MatDialog)/private dialogService = inject(DialogService)/g' /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/timesheets/timesheet-approvals.component.ts

# File 10: timesheet-detail.component.ts
echo "Migrating timesheet-detail.component.ts..."
sed -i 's/import { MatDialogModule, MatDialog } from/import { DialogService } from/g' /workspaces/HRAPP/hrms-frontend/src/app/features/employee/timesheets/timesheet-detail.component.ts
sed -i "s/'@angular\/material\/dialog';/'@app\/shared\/ui';/g" /workspaces/HRAPP/hrms-frontend/src/app/features/employee/timesheets/timesheet-detail.component.ts
sed -i 's/MatDialogModule,//g' /workspaces/HRAPP/hrms-frontend/src/app/features/employee/timesheets/timesheet-detail.component.ts
sed -i 's/private dialog = inject(MatDialog)/private dialogService = inject(DialogService)/g' /workspaces/HRAPP/hrms-frontend/src/app/features/employee/timesheets/timesheet-detail.component.ts

# File 11: salary-components.component.ts
echo "Migrating salary-components.component.ts..."
sed -i 's/import { MatDialogModule, MatDialog } from/import { DialogService } from/g' /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/payroll/salary-components.component.ts
sed -i "s/'@angular\/material\/dialog';/'@app\/shared\/ui';/g" /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/payroll/salary-components.component.ts
sed -i 's/MatDialogModule,//g' /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/payroll/salary-components.component.ts
sed -i 's/private dialog = inject(MatDialog)/private dialogService = inject(DialogService)/g' /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/payroll/salary-components.component.ts

# File 12: department-list.component.ts
echo "Migrating department-list.component.ts..."
sed -i 's/import { MatDialogModule, MatDialog } from/import { DialogService } from/g' /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/organization/departments/department-list.component.ts
sed -i "s/'@angular\/material\/dialog';/'@app\/shared\/ui';/g" /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/organization/departments/department-list.component.ts
sed -i 's/MatDialogModule,//g' /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/organization/departments/department-list.component.ts
sed -i 's/private dialog = inject(MatDialog)/private dialogService = inject(DialogService)/g' /workspaces/HRAPP/hrms-frontend/src/app/features/tenant/organization/departments/department-list.component.ts

echo "=== Migration Complete ==="
echo "Files migrated: 8"
echo "Next: Migrate dialog content components (tier-upgrade, payment-detail, session-timeout)"
