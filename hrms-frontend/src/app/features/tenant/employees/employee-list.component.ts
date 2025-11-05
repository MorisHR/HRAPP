import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { EmployeeService } from '../../../core/services/employee.service';
import { Employee } from '../../../core/models/employee.model';

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="employee-list-container">
      <mat-card>
        <mat-card-header>
          <mat-card-title>
            <h1>Employee Management</h1>
          </mat-card-title>
          <div class="header-actions">
            <button mat-raised-button color="primary" routerLink="/tenant/employees/new">
              <mat-icon>person_add</mat-icon>
              Add New Employee
            </button>
          </div>
        </mat-card-header>

        <mat-card-content>
          @if (loading()) {
            <div class="loading-spinner">
              <mat-spinner></mat-spinner>
              <p>Loading employees...</p>
            </div>
          } @else if (employees().length === 0) {
            <div class="no-data">
              <mat-icon>people_outline</mat-icon>
              <h3>No Employees Found</h3>
              <p>Get started by adding your first employee</p>
              <button mat-raised-button color="primary" routerLink="/tenant/employees/new">
                <mat-icon>add</mat-icon>
                Add Employee
              </button>
            </div>
          } @else {
            <table mat-table [dataSource]="employees()" class="employee-table">
              <ng-container matColumnDef="employeeCode">
                <th mat-header-cell *matHeaderCellDef>Employee Code</th>
                <td mat-cell *matCellDef="let employee">{{ employee.employeeCode }}</td>
              </ng-container>

              <ng-container matColumnDef="fullName">
                <th mat-header-cell *matHeaderCellDef>Full Name</th>
                <td mat-cell *matCellDef="let employee">{{ employee.fullName }}</td>
              </ng-container>

              <ng-container matColumnDef="email">
                <th mat-header-cell *matHeaderCellDef>Email</th>
                <td mat-cell *matCellDef="let employee">{{ employee.email }}</td>
              </ng-container>

              <ng-container matColumnDef="department">
                <th mat-header-cell *matHeaderCellDef>Department</th>
                <td mat-cell *matCellDef="let employee">{{ employee.department || 'N/A' }}</td>
              </ng-container>

              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef>Actions</th>
                <td mat-cell *matCellDef="let employee">
                  <button mat-icon-button [routerLink]="['/tenant/employees', employee.id]">
                    <mat-icon>edit</mat-icon>
                  </button>
                  <button mat-icon-button color="warn" (click)="deleteEmployee(employee.id)">
                    <mat-icon>delete</mat-icon>
                  </button>
                </td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
            </table>
          }
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .employee-list-container {
      padding: 24px;
    }

    mat-card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;
    }

    .header-actions {
      margin-left: auto;
    }

    .loading-spinner {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px;
      gap: 16px;
    }

    .no-data {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px;
      gap: 16px;
      text-align: center;

      mat-icon {
        font-size: 64px;
        width: 64px;
        height: 64px;
        color: #999;
      }

      h3 {
        margin: 0;
        color: #666;
      }

      p {
        margin: 0;
        color: #999;
      }
    }

    .employee-table {
      width: 100%;
    }
  `]
})
export class EmployeeListComponent implements OnInit {
  private employeeService = inject(EmployeeService);

  employees = signal<Employee[]>([]);
  loading = signal(false);
  displayedColumns = ['employeeCode', 'fullName', 'email', 'department', 'actions'];

  ngOnInit(): void {
    console.log('📋 Employee List Component initialized');
    this.loadEmployees();
  }

  loadEmployees(): void {
    this.loading.set(true);
    this.employeeService.getEmployees().subscribe({
      next: (data: any) => {
        console.log('✅ Employees loaded:', data);
        // Handle the API response format: { success: true, data: [...] }
        const employees = data.data || data;
        this.employees.set(employees);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('❌ Error loading employees:', error);
        this.loading.set(false);
      }
    });
  }

  deleteEmployee(id: string): void {
    if (confirm('Are you sure you want to delete this employee?')) {
      this.employeeService.deleteEmployee(id).subscribe({
        next: () => {
          console.log('✅ Employee deleted');
          this.loadEmployees();
        },
        error: (error) => {
          console.error('❌ Error deleting employee:', error);
          alert('Failed to delete employee');
        }
      });
    }
  }
}
