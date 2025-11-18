import { Component, OnInit, signal, computed, inject } from '@angular/core';

import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { UiModule } from '../../../shared/ui/ui.module';
import { ToastService, TableComponent, TableColumn, TableColumnDirective, TooltipDirective } from '../../../shared/ui';
import { Chip } from '@app/shared/ui';
import { MatCardModule } from '@angular/material/card';
import { LocationService } from '../../../core/services/location.service';
import { Location, LocationType, LocationFilter } from '../../../core/models/location';
import { MAURITIUS_DISTRICTS, LOCATION_TYPES } from '../../../core/constants/mauritius.constants';

/**
 * Location List Component
 * Admin dashboard for managing all locations in Mauritius
 *
 * Features:
 * - Table view of all locations
 * - Filter by district, type
 * - Search by name
 * - Add/Edit/Delete actions
 * - Seed database button (one-time)
 * - Material Design UI
 * - Responsive layout
 */
@Component({
  selector: 'app-location-list',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    UiModule,
    Chip,
    TooltipDirective,
    MatCardModule,
    TableComponent,
    TableColumnDirective
],
  template: `
    <div class="location-list-container">
      <!-- Header -->
      <div class="header">
        <div class="title-section">
          <h1>Location Management</h1>
          <p class="subtitle">Manage all locations across Mauritius</p>
        </div>
        <div class="action-buttons">
          <button
            mat-raised-button
            color="accent"
            (click)="seedLocations()"
            [disabled]="loading()"
            >
            <mat-icon>cloud_upload</mat-icon>
            Seed Mauritius Data
          </button>
          <button
            mat-raised-button
            color="primary"
            (click)="createLocation()"
            >
            <mat-icon>add</mat-icon>
            Add Location
          </button>
        </div>
      </div>
    
      <!-- Filters -->
      <mat-card class="filters-card">
        <div class="filters">
          <mat-form-field appearance="outline" class="filter-field">
            <mat-label>Search</mat-label>
            <input
              matInput
              [(ngModel)]="searchTerm"
              (ngModelChange)="applyFilters()"
              placeholder="Search by location name..."
              />
              <mat-icon matPrefix>search</mat-icon>
            </mat-form-field>
    
            <mat-form-field appearance="outline" class="filter-field">
              <mat-label>District</mat-label>
              <mat-select
                [(ngModel)]="selectedDistrict"
                (ngModelChange)="applyFilters()"
                >
                <mat-option [value]="null">All Districts</mat-option>
                @for (district of districts; track district) {
                  <mat-option [value]="district">
                    {{ district }}
                  </mat-option>
                }
              </mat-select>
              <mat-icon matPrefix>location_on</mat-icon>
            </mat-form-field>
    
            <mat-form-field appearance="outline" class="filter-field">
              <mat-label>Type</mat-label>
              <mat-select
                [(ngModel)]="selectedType"
                (ngModelChange)="applyFilters()"
                >
                <mat-option [value]="null">All Types</mat-option>
                @for (type of locationTypes; track type) {
                  <mat-option [value]="type.value">
                    {{ type.label }}
                  </mat-option>
                }
              </mat-select>
              <mat-icon matPrefix>category</mat-icon>
            </mat-form-field>
    
            <button
              mat-stroked-button
              (click)="clearFilters()"
              class="clear-button"
              >
              <mat-icon>clear</mat-icon>
              Clear Filters
            </button>
          </div>
        </mat-card>
    
        <!-- Loading State -->
        @if (loading()) {
          <div class="loading-container">
            <app-progress-spinner size="large" color="primary"></app-progress-spinner>
            <p>Loading locations...</p>
          </div>
        }
    
        <!-- Error State -->
        @if (errorMessage() && !loading()) {
          <mat-card class="error-card">
            <mat-icon color="warn">error</mat-icon>
            <p>{{ errorMessage() }}</p>
            <button mat-raised-button color="primary" (click)="loadLocations()">
              <mat-icon>refresh</mat-icon>
              Retry
            </button>
          </mat-card>
        }
    
        <!-- Data Table -->
        @if (!loading() && !errorMessage()) {
          <mat-card class="table-card">
            <!-- Stats -->
            <div class="stats">
              <div class="stat">
                <span class="stat-value">{{ filteredLocations().length }}</span>
                <span class="stat-label">Total Locations</span>
              </div>
              <div class="stat">
                <span class="stat-value">{{ activeCount() }}</span>
                <span class="stat-label">Active</span>
              </div>
              <div class="stat">
                <span class="stat-value">{{ uniqueDistricts() }}</span>
                <span class="stat-label">Districts</span>
              </div>
            </div>
    
            <!-- Table -->
            <div class="table-container">
              <app-table
                [data]="filteredLocations()"
                [columns]="columns"
                [loading]="loading()"
                [hoverable]="true">

                <!-- Custom template for name column -->
                <ng-template appTableColumn="name" let-row>
                  <strong>{{ row.name }}</strong>
                </ng-template>

                <!-- Custom template for type column -->
                <ng-template appTableColumn="type" let-row>
                  <app-chip [label]="row.type" [color]="'neutral'" [style.background-color]="getTypeColor(row.type)" />
                </ng-template>

                <!-- Custom template for region column -->
                <ng-template appTableColumn="region" let-row>
                  {{ row.region || '-' }}
                </ng-template>

                <!-- Custom template for postalCode column -->
                <ng-template appTableColumn="postalCode" let-row>
                  {{ row.postalCode || '-' }}
                </ng-template>

                <!-- Custom template for status column -->
                <ng-template appTableColumn="status" let-row>
                  <app-chip [label]="row.isActive ? 'Active' : 'Inactive'" [color]="row.isActive ? 'success' : 'error'" />
                </ng-template>

                <!-- Custom template for actions column -->
                <ng-template appTableColumn="actions" let-row>
                  <div class="action-buttons">
                    <button
                      mat-icon-button
                      color="primary"
                      (click)="editLocation(row)"
                      appTooltip="Edit location">
                      <mat-icon>edit</mat-icon>
                    </button>
                    <button
                      mat-icon-button
                      color="warn"
                      (click)="deleteLocation(row)"
                      appTooltip="Delete location">
                      <mat-icon>delete</mat-icon>
                    </button>
                  </div>
                </ng-template>
              </app-table>

              <!-- Empty State -->
              @if (!loading() && filteredLocations().length === 0) {
                <div class="no-data">
                  <mat-icon>search_off</mat-icon>
                  <p>No locations found</p>
                  <p class="hint">Try adjusting your filters or add a new location</p>
                </div>
              }
            </div>
          </mat-card>
        }
      </div>
    `,
  styles: [`
    .location-list-container {
      padding: 24px;
      max-width: 1400px;
      margin: 0 auto;
    }

    .header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 24px;
      gap: 16px;
      flex-wrap: wrap;
    }

    .title-section h1 {
      margin: 0;
      font-size: 28px;
      font-weight: 500;
      color: #1a1a1a;
    }

    .subtitle {
      margin: 4px 0 0 0;
      color: #666;
      font-size: 14px;
    }

    .action-buttons {
      display: flex;
      gap: 12px;
    }

    .action-buttons button {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .filters-card {
      margin-bottom: 24px;
    }

    .filters {
      display: grid;
      grid-template-columns: 2fr 1fr 1fr auto;
      gap: 16px;
      align-items: center;
    }

    .filter-field {
      width: 100%;
    }

    .clear-button {
      height: 56px;
    }

    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 64px 24px;
      gap: 16px;
    }

    .error-card {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px 24px;
      gap: 16px;
      background-color: #ffebee;
    }

    .error-card mat-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
    }

    .table-card {
      overflow: hidden;
    }

    .stats {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
      gap: 24px;
      padding: 24px;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      margin: -16px -16px 24px -16px;
    }

    .stat {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .stat-value {
      font-size: 32px;
      font-weight: 600;
    }

    .stat-label {
      font-size: 14px;
      opacity: 0.9;
    }

    .table-container {
      overflow-x: auto;
    }

    .locations-table {
      width: 100%;
    }

    .locations-table th {
      font-weight: 600;
      color: #1a1a1a;
    }

    .action-buttons {
      display: flex;
      gap: 8px;
    }

    .no-data {
      text-align: center;
      padding: 48px 24px !important;
    }

    .no-data mat-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #ccc;
    }

    .no-data p {
      margin: 16px 0 4px 0;
      font-size: 16px;
      color: #666;
    }

    .no-data .hint {
      font-size: 14px;
      color: #999;
    }

    app-chip {
      font-size: 12px;
    }

    @media (max-width: 768px) {
      .filters {
        grid-template-columns: 1fr;
      }

      .header {
        flex-direction: column;
      }

      .action-buttons {
        width: 100%;
        flex-direction: column;
      }

      .action-buttons button {
        width: 100%;
      }

      .stats {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class LocationListComponent implements OnInit {
  private locationService = inject(LocationService);
  private router = inject(Router);
  private toastService = inject(ToastService);

  // Table columns
  columns: TableColumn[] = [
    { key: 'name', label: 'Name' },
    { key: 'district', label: 'District' },
    { key: 'type', label: 'Type' },
    { key: 'region', label: 'Region' },
    { key: 'postalCode', label: 'Postal Code' },
    { key: 'status', label: 'Status' },
    { key: 'actions', label: 'Actions' }
  ];

  // Constants
  districts = MAURITIUS_DISTRICTS;
  locationTypes = LOCATION_TYPES;

  // Signals
  loading = signal<boolean>(false);
  errorMessage = signal<string>('');
  locations = signal<Location[]>([]);
  filteredLocations = signal<Location[]>([]);

  // Filter state
  searchTerm = '';
  selectedDistrict: string | null = null;
  selectedType: string | null = null;

  // Computed values
  activeCount = computed(() =>
    this.filteredLocations().filter(loc => loc.isActive).length
  );

  uniqueDistricts = computed(() =>
    new Set(this.filteredLocations().map(loc => loc.district)).size
  );

  ngOnInit(): void {
    this.loadLocations();
  }

  /**
   * Load all locations from the API
   */
  loadLocations(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.locationService.getLocations().subscribe({
      next: (locations) => {
        this.locations.set(locations);
        this.filteredLocations.set(locations);
        this.loading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.message || 'Failed to load locations');
        this.loading.set(false);
        this.toastService.error('Failed to load locations', 5000);
      }
    });
  }

  /**
   * Apply filters to the location list
   */
  applyFilters(): void {
    let filtered = [...this.locations()];

    // Search filter
    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(loc =>
        loc.name.toLowerCase().includes(term) ||
        loc.district.toLowerCase().includes(term) ||
        (loc.region && loc.region.toLowerCase().includes(term))
      );
    }

    // District filter
    if (this.selectedDistrict) {
      filtered = filtered.filter(loc => loc.district === this.selectedDistrict);
    }

    // Type filter
    if (this.selectedType) {
      filtered = filtered.filter(loc => loc.type === this.selectedType);
    }

    this.filteredLocations.set(filtered);
  }

  /**
   * Clear all filters
   */
  clearFilters(): void {
    this.searchTerm = '';
    this.selectedDistrict = null;
    this.selectedType = null;
    this.filteredLocations.set(this.locations());
  }

  /**
   * Navigate to create location page
   */
  createLocation(): void {
    this.router.navigate(['/admin/locations/new']);
  }

  /**
   * Navigate to edit location page
   */
  editLocation(location: Location): void {
    this.router.navigate(['/admin/locations', location.id, 'edit']);
  }

  /**
   * Delete a location with confirmation
   */
  deleteLocation(location: Location): void {
    const confirmed = confirm(
      `Are you sure you want to delete "${location.name}"?\n\nThis action cannot be undone.`
    );

    if (!confirmed) return;

    this.locationService.deleteLocation(location.id).subscribe({
      next: () => {
        this.toastService.success('Location deleted successfully', 3000);
        this.loadLocations();
      },
      error: (error) => {
        this.toastService.error(
          error.message || 'Failed to delete location',
          5000
        );
      }
    });
  }

  /**
   * Seed Mauritius locations (one-time operation)
   */
  seedLocations(): void {
    const confirmed = confirm(
      'This will populate the database with all Mauritius locations.\n\n' +
      'This is a one-time operation and should only be run on a fresh database.\n\n' +
      'Continue?'
    );

    if (!confirmed) return;

    this.loading.set(true);

    this.locationService.seedMauritiusLocations().subscribe({
      next: (response) => {
        this.toastService.success(
          `Successfully seeded ${response.data.totalSeeded} locations across ${response.data.districts.length} districts`,
          5000
        );
        this.loadLocations();
      },
      error: (error) => {
        this.loading.set(false);
        this.toastService.error(
          error.message || 'Failed to seed locations',
          5000
        );
      }
    });
  }

  /**
   * Get color for location type chip
   */
  getTypeColor(type: LocationType): string {
    const colors: Record<LocationType, string> = {
      [LocationType.City]: '#e3f2fd',
      [LocationType.Town]: '#f3e5f5',
      [LocationType.Village]: '#e8f5e9',
      [LocationType.Suburb]: '#fff3e0',
      [LocationType.Other]: '#f5f5f5'
    };
    return colors[type] || '#f5f5f5';
  }
}
