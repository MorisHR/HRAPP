import { Component, OnInit, signal, inject } from '@angular/core';

import { Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { LocationService } from '../../../core/services/location.service';
import {
  Location,
  LocationType,
  CreateLocationRequest,
  UpdateLocationRequest
} from '../../../core/models/location';
import { MAURITIUS_DISTRICTS, LOCATION_TYPES } from '../../../core/constants/mauritius.constants';

/**
 * Location Form Component
 * Create and edit locations for Mauritius
 *
 * Features:
 * - Create/Edit location
 * - Validation (required fields, district list)
 * - District dropdown (9 Mauritius districts)
 * - Type selection
 * - Postal code validation
 * - Google Maps integration support (latitude/longitude)
 * - Active/inactive toggle
 * - Material Design UI
 */
@Component({
  selector: 'app-location-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatSlideToggleModule,
    MatTooltipModule
],
  template: `
    <div class="location-form-container">
      <!-- Header -->
      <div class="header">
        <button mat-icon-button (click)="goBack()">
          <mat-icon>arrow_back</mat-icon>
        </button>
        <div class="title-section">
          <h1>{{ isEditMode() ? 'Edit Location' : 'Create Location' }}</h1>
          <p class="subtitle">
            {{ isEditMode() ? 'Update location details' : 'Add a new location to Mauritius' }}
          </p>
        </div>
      </div>
    
      <!-- Loading State -->
      @if (loading()) {
        <div class="loading-container">
          <mat-spinner diameter="50"></mat-spinner>
          <p>{{ isEditMode() ? 'Loading location...' : 'Creating location...' }}</p>
        </div>
      }
    
      <!-- Form -->
      @if (!loading()) {
        <mat-card class="form-card">
          <form [formGroup]="locationForm" (ngSubmit)="onSubmit()">
            <!-- Basic Information -->
            <div class="form-section">
              <h2>Basic Information</h2>
    
              <div class="form-row">
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Location Name</mat-label>
                  <input
                    matInput
                    formControlName="name"
                    placeholder="e.g., Port Louis, Curepipe, Rose Hill"
                    required
                    />
                    <mat-icon matPrefix>place</mat-icon>
                    @if (locationForm.get('name')?.hasError('required') && locationForm.get('name')?.touched) {
                      <mat-error>Location name is required</mat-error>
                    }
                  </mat-form-field>
                </div>
    
                <div class="form-row">
                  <mat-form-field appearance="outline" class="half-width">
                    <mat-label>District</mat-label>
                    <mat-select formControlName="district" required>
                      @for (district of districts; track district) {
                        <mat-option [value]="district">
                          {{ district }}
                        </mat-option>
                      }
                    </mat-select>
                    <mat-icon matPrefix>location_on</mat-icon>
                    @if (locationForm.get('district')?.hasError('required') && locationForm.get('district')?.touched) {
                      <mat-error>District is required</mat-error>
                    }
                  </mat-form-field>
    
                  <mat-form-field appearance="outline" class="half-width">
                    <mat-label>Type</mat-label>
                    <mat-select formControlName="type" required>
                      @for (type of locationTypes; track type) {
                        <mat-option [value]="type.value">
                          {{ type.label }}
                        </mat-option>
                      }
                    </mat-select>
                    <mat-icon matPrefix>category</mat-icon>
                    @if (locationForm.get('type')?.hasError('required') && locationForm.get('type')?.touched) {
                      <mat-error>Type is required</mat-error>
                    }
                  </mat-form-field>
                </div>
              </div>
    
              <!-- Additional Details -->
              <div class="form-section">
                <h2>Additional Details</h2>
    
                <div class="form-row">
                  <mat-form-field appearance="outline" class="half-width">
                    <mat-label>Region (Optional)</mat-label>
                    <input
                      matInput
                      formControlName="region"
                      placeholder="e.g., North, South, East, West"
                      />
                      <mat-icon matPrefix>map</mat-icon>
                    </mat-form-field>
    
                    <mat-form-field appearance="outline" class="half-width">
                      <mat-label>Postal Code (Optional)</mat-label>
                      <input
                        matInput
                        formControlName="postalCode"
                        placeholder="e.g., 11302"
                        pattern="^[0-9]{5}$"
                        />
                        <mat-icon matPrefix>local_post_office</mat-icon>
                        @if (locationForm.get('postalCode')?.hasError('pattern')) {
                          <mat-error>Invalid postal code format (5 digits)</mat-error>
                        }
                      </mat-form-field>
                    </div>
                  </div>
    
                  <!-- Coordinates (Optional) -->
                  <div class="form-section">
                    <h2>
                      Coordinates (Optional)
                      <mat-icon
                        matTooltip="Use Google Maps to find precise coordinates"
                        class="info-icon"
                        >
                        info
                      </mat-icon>
                    </h2>
    
                    <div class="form-row">
                      <mat-form-field appearance="outline" class="half-width">
                        <mat-label>Latitude</mat-label>
                        <input
                          matInput
                          formControlName="latitude"
                          type="number"
                          step="0.000001"
                          placeholder="e.g., -20.1644"
                          />
                          <mat-icon matPrefix>my_location</mat-icon>
                          @if (locationForm.get('latitude')?.hasError('min') || locationForm.get('latitude')?.hasError('max')) {
                            <mat-error>Latitude must be between -90 and 90</mat-error>
                          }
                        </mat-form-field>
    
                        <mat-form-field appearance="outline" class="half-width">
                          <mat-label>Longitude</mat-label>
                          <input
                            matInput
                            formControlName="longitude"
                            type="number"
                            step="0.000001"
                            placeholder="e.g., 57.5024"
                            />
                            <mat-icon matPrefix>explore</mat-icon>
                            @if (locationForm.get('longitude')?.hasError('min') || locationForm.get('longitude')?.hasError('max')) {
                              <mat-error>Longitude must be between -180 and 180</mat-error>
                            }
                          </mat-form-field>
                        </div>
                      </div>
    
                      <!-- Status -->
                      <div class="form-section">
                        <h2>Status</h2>
    
                        <div class="toggle-row">
                          <mat-slide-toggle formControlName="isActive" color="primary">
                            <span class="toggle-label">
                              {{ locationForm.get('isActive')?.value ? 'Active' : 'Inactive' }}
                            </span>
                          </mat-slide-toggle>
                          <p class="toggle-hint">
                            {{ locationForm.get('isActive')?.value
                            ? 'This location is active and can be selected in forms'
                            : 'This location is inactive and will not appear in dropdowns'
                            }}
                          </p>
                        </div>
                      </div>
    
                      <!-- Actions -->
                      <div class="form-actions">
                        <button
                          type="button"
                          mat-stroked-button
                          (click)="goBack()"
                          [disabled]="submitting()"
                          >
                          <mat-icon>cancel</mat-icon>
                          Cancel
                        </button>
                        <button
                          type="submit"
                          mat-raised-button
                          color="primary"
                          [disabled]="locationForm.invalid || submitting()"
                          >
                          @if (submitting()) {
                            <mat-spinner diameter="20"></mat-spinner>
                          }
                          @if (!submitting()) {
                            <mat-icon>{{ isEditMode() ? 'save' : 'add' }}</mat-icon>
                          }
                          {{ isEditMode() ? 'Update Location' : 'Create Location' }}
                        </button>
                      </div>
                    </form>
                  </mat-card>
                }
              </div>
    `,
  styles: [`
    .location-form-container {
      padding: 24px;
      max-width: 900px;
      margin: 0 auto;
    }

    .header {
      display: flex;
      align-items: flex-start;
      gap: 16px;
      margin-bottom: 24px;
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

    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 64px 24px;
      gap: 16px;
    }

    .form-card {
      padding: 32px;
    }

    .form-section {
      margin-bottom: 32px;
    }

    .form-section h2 {
      font-size: 18px;
      font-weight: 500;
      color: #1a1a1a;
      margin: 0 0 16px 0;
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .info-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
      color: #666;
      cursor: help;
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
      margin-bottom: 16px;
    }

    .full-width {
      grid-column: 1 / -1;
      width: 100%;
    }

    .half-width {
      width: 100%;
    }

    .toggle-row {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .toggle-label {
      font-weight: 500;
      font-size: 16px;
    }

    .toggle-hint {
      margin: 0;
      font-size: 14px;
      color: #666;
      padding-left: 52px;
    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 16px;
      margin-top: 32px;
      padding-top: 24px;
      border-top: 1px solid #e0e0e0;
    }

    .form-actions button {
      display: flex;
      align-items: center;
      gap: 8px;
      min-width: 150px;
    }

    mat-form-field {
      display: block;
    }

    @media (max-width: 768px) {
      .location-form-container {
        padding: 16px;
      }

      .form-card {
        padding: 16px;
      }

      .form-row {
        grid-template-columns: 1fr;
      }

      .form-actions {
        flex-direction: column-reverse;
      }

      .form-actions button {
        width: 100%;
      }
    }
  `]
})
export class LocationFormComponent implements OnInit {
  private locationService = inject(LocationService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);
  private snackBar = inject(MatSnackBar);

  // Constants
  districts = MAURITIUS_DISTRICTS;
  locationTypes = LOCATION_TYPES;

  // Signals
  loading = signal<boolean>(false);
  submitting = signal<boolean>(false);
  isEditMode = signal<boolean>(false);

  // Form
  locationForm: FormGroup;
  locationId: string | null = null;

  constructor() {
    this.locationForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      district: ['', Validators.required],
      type: ['', Validators.required],
      region: [''],
      postalCode: ['', [Validators.pattern(/^[0-9]{5}$/)]],
      latitude: ['', [Validators.min(-90), Validators.max(90)]],
      longitude: ['', [Validators.min(-180), Validators.max(180)]],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    // Check if we're in edit mode
    this.locationId = this.route.snapshot.paramMap.get('id');

    if (this.locationId) {
      this.isEditMode.set(true);
      this.loadLocation(this.locationId);
    }
  }

  /**
   * Load location data for editing
   */
  loadLocation(id: string): void {
    this.loading.set(true);

    this.locationService.getLocationById(id).subscribe({
      next: (location) => {
        this.locationForm.patchValue({
          name: location.name,
          district: location.district,
          type: location.type,
          region: location.region || '',
          postalCode: location.postalCode || '',
          latitude: location.latitude || '',
          longitude: location.longitude || '',
          isActive: location.isActive
        });
        this.loading.set(false);
      },
      error: (error) => {
        this.snackBar.open(
          error.message || 'Failed to load location',
          'Close',
          { duration: 5000, panelClass: ['error-snackbar'] }
        );
        this.loading.set(false);
        this.goBack();
      }
    });
  }

  /**
   * Submit form
   */
  onSubmit(): void {
    if (this.locationForm.invalid) {
      this.locationForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);

    const formValue = this.locationForm.value;

    // Convert empty strings to null for optional fields
    const locationData = {
      name: formValue.name,
      district: formValue.district,
      type: formValue.type as LocationType,
      region: formValue.region || undefined,
      postalCode: formValue.postalCode || undefined,
      latitude: formValue.latitude ? Number(formValue.latitude) : undefined,
      longitude: formValue.longitude ? Number(formValue.longitude) : undefined,
      isActive: formValue.isActive
    };

    if (this.isEditMode() && this.locationId) {
      this.updateLocation(this.locationId, locationData);
    } else {
      this.createLocation(locationData);
    }
  }

  /**
   * Create new location
   */
  private createLocation(data: CreateLocationRequest): void {
    this.locationService.createLocation(data).subscribe({
      next: (location) => {
        this.snackBar.open('Location created successfully', 'Close', {
          duration: 3000,
          panelClass: ['success-snackbar']
        });
        this.submitting.set(false);
        this.router.navigate(['/admin/locations']);
      },
      error: (error) => {
        this.snackBar.open(
          error.message || 'Failed to create location',
          'Close',
          { duration: 5000, panelClass: ['error-snackbar'] }
        );
        this.submitting.set(false);
      }
    });
  }

  /**
   * Update existing location
   */
  private updateLocation(id: string, data: UpdateLocationRequest): void {
    this.locationService.updateLocation(id, data).subscribe({
      next: (location) => {
        this.snackBar.open('Location updated successfully', 'Close', {
          duration: 3000,
          panelClass: ['success-snackbar']
        });
        this.submitting.set(false);
        this.router.navigate(['/admin/locations']);
      },
      error: (error) => {
        this.snackBar.open(
          error.message || 'Failed to update location',
          'Close',
          { duration: 5000, panelClass: ['error-snackbar'] }
        );
        this.submitting.set(false);
      }
    });
  }

  /**
   * Navigate back to location list
   */
  goBack(): void {
    this.router.navigate(['/admin/locations']);
  }
}
