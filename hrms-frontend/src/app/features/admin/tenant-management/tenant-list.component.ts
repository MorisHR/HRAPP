import { Component, signal, inject, OnInit, computed, ChangeDetectionStrategy } from '@angular/core';

import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Chip, ChipColor } from '@app/shared/ui';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterModule } from '@angular/router';
import { MenuComponent, MenuItem } from '../../../shared/ui';
import { UiModule } from '../../../shared/ui/ui.module';
import { TenantService } from '../../../core/services/tenant.service';
import { Tenant, TenantStatus } from '../../../core/models/tenant.model';
import { TableComponent, TableColumn } from '../../../shared/ui/components/table/table';

@Component({
  selector: 'app-tenant-list',
  standalone: true,
  imports: [
    TableComponent,
    MatButtonModule,
    MatIconModule,
    Chip,
    MatToolbarModule,
    MatFormFieldModule,
    MatInputModule,
    MenuComponent,
    UiModule,
    RouterModule
],
  templateUrl: './tenant-list.component.html',
  styleUrl: './tenant-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantListComponent implements OnInit {
  private tenantService = inject(TenantService);
  private router = inject(Router);

  // Table data
  tenants = signal<Tenant[]>([]);
  searchTerm = signal<string>('');

  loading = this.tenantService.loading;

  // Table columns
  tableColumns: TableColumn[] = [
    { key: 'companyName', label: 'Name', sortable: true },
    { key: 'subdomain', label: 'Domain', sortable: true },
    { key: 'employeeTierDisplay', label: 'Tier', sortable: true },
    { key: 'employeeCount', label: 'Employees', sortable: true },
    { key: 'status', label: 'Status', sortable: true },
    { key: 'actions', label: 'Actions', sortable: false }
  ];

  // Filtered data based on search
  filteredTenants = computed(() => {
    const search = this.searchTerm().toLowerCase();
    if (!search) return this.tenants();

    return this.tenants().filter(tenant =>
      tenant.companyName?.toLowerCase().includes(search) ||
      tenant.subdomain?.toLowerCase().includes(search)
    );
  });

  ngOnInit(): void {
    this.loadTenants();
  }

  private loadTenants(): void {
    this.tenantService.getTenants().subscribe({
      next: (tenants) => {
        this.tenants.set(tenants);
      }
    });
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.searchTerm.set(filterValue.trim());
  }

  getStatusColor(status: TenantStatus): ChipColor {
    switch (status) {
      case TenantStatus.Active:
        return 'success';
      case TenantStatus.Trial:
        return 'warning';
      case TenantStatus.Suspended:
        return 'error';
      default:
        return 'neutral';
    }
  }

  getTenantMenuItems(tenant: Tenant): MenuItem[] {
    return [
      { label: 'View Details', value: 'view', icon: 'visibility' },
      { label: 'Edit', value: 'edit', icon: 'edit' },
      { label: 'Suspend', value: 'suspend', icon: 'block' },
      { label: 'Delete', value: 'delete', icon: 'delete' }
    ];
  }

  handleTenantMenuClick(value: string, tenant: Tenant): void {
    if (value === 'view') {
      this.router.navigate(['/admin/tenants', tenant.id]);
    } else if (value === 'edit') {
      this.router.navigate(['/admin/tenants', tenant.id, 'edit']);
    } else if (value === 'suspend') {
      this.suspendTenant(tenant.id);
    } else if (value === 'delete') {
      this.deleteTenant(tenant.id);
    }
  }

  suspendTenant(id: string): void {
    if (confirm('Are you sure you want to suspend this tenant?')) {
      this.tenantService.suspendTenant(id).subscribe({
        next: () => {
          this.loadTenants();
        }
      });
    }
  }

  deleteTenant(id: string): void {
    if (confirm('Are you sure you want to delete this tenant? This action cannot be undone.')) {
      this.tenantService.deleteTenant(id).subscribe({
        next: () => {
          this.loadTenants();
        }
      });
    }
  }
}
