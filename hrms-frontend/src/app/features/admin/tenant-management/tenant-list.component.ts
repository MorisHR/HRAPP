import { Component, signal, inject, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { RouterModule } from '@angular/router';
import { TenantService } from '../../../core/services/tenant.service';
import { Tenant, TenantStatus } from '../../../core/models/tenant.model';

@Component({
  selector: 'app-tenant-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatToolbarModule,
    MatFormFieldModule,
    MatInputModule,
    MatMenuModule,
    RouterModule
  ],
  templateUrl: './tenant-list.component.html',
  styleUrl: './tenant-list.component.scss'
})
export class TenantListComponent implements OnInit {
  private tenantService = inject(TenantService);

  displayedColumns: string[] = ['name', 'domain', 'industry', 'employeeCount', 'status', 'actions'];
  dataSource = new MatTableDataSource<Tenant>([]);

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  loading = this.tenantService.loading;

  ngOnInit(): void {
    this.loadTenants();
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }

  private loadTenants(): void {
    this.tenantService.getTenants().subscribe({
      next: (tenants) => {
        this.dataSource.data = tenants;
      }
    });
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();

    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
  }

  getStatusColor(status: TenantStatus): string {
    switch (status) {
      case TenantStatus.Active:
        return 'primary';
      case TenantStatus.Trial:
        return 'accent';
      case TenantStatus.Suspended:
        return 'warn';
      default:
        return '';
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
