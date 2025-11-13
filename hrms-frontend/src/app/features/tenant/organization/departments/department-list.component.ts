import { Component, OnInit, signal, inject } from '@angular/core';

import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { DepartmentService, DepartmentDto } from './services/department.service';

@Component({
  selector: 'app-department-list',
  standalone: true,
  imports: [
    RouterModule,
    FormsModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatChipsModule,
    MatTooltipModule,
    MatDialogModule
],
  template: `
    <div class="department-list-container">
      <div class="page-header">
        <div class="header-content">
          <h1>Departments</h1>
          <p class="subtitle">Manage organizational departments and hierarchy</p>
        </div>
        <button mat-raised-button color="primary" routerLink="add">
          <mat-icon>add</mat-icon>
          Add Department
        </button>
      </div>

      <!-- Search Box -->
      @if (!loading() && !error()) {
        <mat-card class="search-card">
          <mat-card-content>
            <mat-form-field appearance="outline" class="search-field">
              <mat-label>Search departments</mat-label>
              <input matInput
                     placeholder="Search by name or code"
                     [(ngModel)]="searchTerm"
                     (input)="filterDepartments()">
              <mat-icon matSuffix>search</mat-icon>
            </mat-form-field>
          </mat-card-content>
        </mat-card>
      }

      <!-- Loading State -->
      @if (loading()) {
        <mat-card>
          <mat-card-content class="loading-container">
            <mat-spinner diameter="50"></mat-spinner>
            <p>Loading departments...</p>
          </mat-card-content>
        </mat-card>
      }

      <!-- Error State -->
      @if (error() && !loading()) {
        <mat-card class="error-card">
          <mat-card-content>
            <div class="error-content">
              <mat-icon color="warn">error_outline</mat-icon>
              <div>
                <h3>Failed to load departments</h3>
                <p>{{ error() }}</p>
              </div>
              <button mat-raised-button color="primary" (click)="loadDepartments()">
                <mat-icon>refresh</mat-icon>
                Retry
              </button>
            </div>
          </mat-card-content>
        </mat-card>
      }

      <!-- Departments Table -->
      @if (!loading() && !error() && filteredDepartments().length > 0) {
        <mat-card>
          <mat-card-content>
            <table mat-table [dataSource]="filteredDepartments()" class="departments-table">

              <!-- Code Column -->
              <ng-container matColumnDef="code">
                <th mat-header-cell *matHeaderCellDef>Code</th>
                <td mat-cell *matCellDef="let dept">
                  <strong>{{ dept.code }}</strong>
                </td>
              </ng-container>

              <!-- Name Column -->
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef>Department Name</th>
                <td mat-cell *matCellDef="let dept">{{ dept.name }}</td>
              </ng-container>

              <!-- Parent Column -->
              <ng-container matColumnDef="parent">
                <th mat-header-cell *matHeaderCellDef>Parent Department</th>
                <td mat-cell *matCellDef="let dept">
                  {{ dept.parentDepartmentName || '—' }}
                </td>
              </ng-container>

              <!-- Head Column -->
              <ng-container matColumnDef="head">
                <th mat-header-cell *matHeaderCellDef>Department Head</th>
                <td mat-cell *matCellDef="let dept">
                  {{ dept.departmentHeadName || '—' }}
                </td>
              </ng-container>

              <!-- Employee Count Column -->
              <ng-container matColumnDef="employeeCount">
                <th mat-header-cell *matHeaderCellDef>Employees</th>
                <td mat-cell *matCellDef="let dept">
                  <mat-chip>{{ dept.employeeCount }}</mat-chip>
                </td>
              </ng-container>

              <!-- Status Column -->
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let dept">
                  <mat-chip [class.active-chip]="dept.isActive" [class.inactive-chip]="!dept.isActive">
                    {{ dept.isActive ? 'Active' : 'Inactive' }}
                  </mat-chip>
                </td>
              </ng-container>

              <!-- Actions Column -->
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef>Actions</th>
                <td mat-cell *matCellDef="let dept">
                  <button mat-icon-button
                          [routerLink]="['edit', dept.id]"
                          matTooltip="Edit department">
                    <mat-icon>edit</mat-icon>
                  </button>
                  <button mat-icon-button
                          color="warn"
                          (click)="deleteDepartment(dept)"
                          matTooltip="Delete department">
                    <mat-icon>delete</mat-icon>
                  </button>
                </td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
            </table>
          </mat-card-content>
        </mat-card>
      }

      <!-- Empty State -->
      @if (!loading() && !error() && filteredDepartments().length === 0 && departments().length > 0) {
        <mat-card class="empty-state">
          <mat-card-content>
            <mat-icon>search_off</mat-icon>
            <h3>No departments found</h3>
            <p>No departments match your search criteria</p>
          </mat-card-content>
        </mat-card>
      }

      @if (!loading() && !error() && departments().length === 0) {
        <mat-card class="empty-state">
          <mat-card-content>
            <mat-icon>business</mat-icon>
            <h3>No departments yet</h3>
            <p>Get started by creating your first department</p>
            <button mat-raised-button color="primary" routerLink="add">
              <mat-icon>add</mat-icon>
              Add Department
            </button>
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .department-list-container {
      padding: 24px;
      max-width: 1400px;
      margin: 0 auto;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 24px;

      .header-content {
        h1 {
          margin: 0;
          font-size: 28px;
          font-weight: 600;
          color: #000000;
        }

        .subtitle {
          margin: 8px 0 0 0;
          color: #666;
          font-size: 14px;
        }
      }
    }

    .search-card {
      margin-bottom: 24px;

      .search-field {
        width: 100%;
        max-width: 500px;
      }
    }

    .departments-table {
      width: 100%;

      th {
        font-weight: 600;
        color: #000000;
      }

      td {
        padding: 16px 8px;
      }

      strong {
        color: #000000;
        font-weight: 600;
      }
    }

    mat-chip {
      font-size: 12px;
      min-height: 24px;
      padding: 4px 12px;
    }

    .active-chip {
      background-color: #4caf50 !important;
      color: white !important;
    }

    .inactive-chip {
      background-color: #f44336 !important;
      color: white !important;
    }

    .loading-container,
    .error-content,
    .empty-state mat-card-content {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px 24px;
      text-align: center;

      mat-icon {
        font-size: 64px;
        width: 64px;
        height: 64px;
        color: #666;
        margin-bottom: 16px;
      }

      h3 {
        margin: 0 0 8px 0;
        font-size: 20px;
        font-weight: 600;
      }

      p {
        margin: 0 0 16px 0;
        color: #666;
      }
    }

    .error-content {
      flex-direction: row;
      gap: 16px;

      mat-icon {
        margin: 0;
      }

      div {
        flex: 1;
        text-align: left;
      }
    }
  `]
})
export class DepartmentListComponent implements OnInit {
  private departmentService = inject(DepartmentService);
  private router = inject(Router);
  private dialog = inject(MatDialog);

  departments = signal<DepartmentDto[]>([]);
  filteredDepartments = signal<DepartmentDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  searchTerm = '';

  displayedColumns = ['code', 'name', 'parent', 'head', 'employeeCount', 'status', 'actions'];

  ngOnInit(): void {
    this.loadDepartments();
  }

  loadDepartments(): void {
    this.loading.set(true);
    this.error.set(null);

    this.departmentService.getAll().subscribe({
      next: (data) => {
        this.departments.set(data);
        this.filteredDepartments.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading departments:', err);
        this.error.set(err.error?.error || 'Failed to load departments');
        this.loading.set(false);
      }
    });
  }

  filterDepartments(): void {
    const term = this.searchTerm.toLowerCase();
    if (!term) {
      this.filteredDepartments.set(this.departments());
      return;
    }

    const filtered = this.departments().filter(dept =>
      dept.name.toLowerCase().includes(term) ||
      dept.code.toLowerCase().includes(term)
    );
    this.filteredDepartments.set(filtered);
  }

  deleteDepartment(dept: DepartmentDto): void {
    const message = dept.employeeCount > 0
      ? `This department has ${dept.employeeCount} employee(s). Are you sure you want to delete "${dept.name}"?`
      : `Are you sure you want to delete "${dept.name}"?`;

    if (!confirm(message)) {
      return;
    }

    this.departmentService.delete(dept.id).subscribe({
      next: () => {
        alert('Department deleted successfully');
        this.loadDepartments();
      },
      error: (err) => {
        alert(err.error?.error || 'Failed to delete department');
      }
    });
  }
}
