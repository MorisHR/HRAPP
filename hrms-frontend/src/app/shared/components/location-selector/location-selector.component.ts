import { Component, Input, Output, EventEmitter, OnInit, forwardRef, signal, inject } from '@angular/core';

import { FormsModule, ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { debounceTime, distinctUntilChanged, switchMap, catchError, of } from 'rxjs';
import { LocationService } from '../../../core/services/location.service';
import { Location } from '../../../core/models/location';
import { MAURITIUS_DISTRICTS } from '../../../core/constants/mauritius.constants';

/**
 * Location Selector Component
 * Reusable component for selecting locations in Mauritius
 *
 * Features:
 * - District dropdown selection
 * - Location autocomplete search
 * - Hierarchical selection (District → Location)
 * - Supports ngModel and Reactive Forms
 * - Loading states
 * - Error handling
 *
 * Usage in Template Forms:
 * <app-location-selector
 *   [(ngModel)]="selectedLocation"
 *   [required]="true"
 *   [disabled]="false"
 * ></app-location-selector>
 *
 * Usage in Reactive Forms:
 * <app-location-selector
 *   formControlName="locationId"
 * ></app-location-selector>
 */
@Component({
  selector: 'app-location-selector',
  standalone: true,
  imports: [
    FormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatAutocompleteModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatIconModule
],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => LocationSelectorComponent),
      multi: true
    }
  ],
  template: `
    <div class="location-selector">
      <!-- District Selection -->
      <mat-form-field appearance="outline" class="full-width">
        <mat-label>District</mat-label>
        <mat-select
          [(ngModel)]="selectedDistrict"
          (ngModelChange)="onDistrictChange($event)"
          [disabled]="isDisabled()"
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
    
      <!-- Location Autocomplete -->
      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Location</mat-label>
        <input
          type="text"
          matInput
          [(ngModel)]="searchTerm"
          (ngModelChange)="onSearchChange($event)"
          [matAutocomplete]="auto"
          [disabled]="isDisabled()"
          placeholder="Search for a location..."
          />
          <mat-icon matPrefix>search</mat-icon>
          @if (loading()) {
            <mat-spinner matSuffix diameter="20"></mat-spinner>
          }
          <mat-autocomplete
            #auto="matAutocomplete"
            [displayWith]="displayLocation"
            (optionSelected)="onLocationSelected($event.option.value)"
            >
            @if (filteredLocations().length === 0 && !loading()) {
              <mat-option disabled>
                {{ searchTerm ? 'No locations found' : 'Start typing to search...' }}
              </mat-option>
            }
            @for (location of filteredLocations(); track location.id) {
              <mat-option [value]="location">
                <div class="location-option">
                  <span class="location-name">{{ location.name }}</span>
                  <span class="location-meta">
                    {{ location.district }} • {{ location.type }}
                  </span>
                </div>
              </mat-option>
            }
          </mat-autocomplete>
        </mat-form-field>
    
        <!-- Error Message -->
        @if (errorMessage()) {
          <div class="error-message">
            <mat-icon>error</mat-icon>
            <span>{{ errorMessage() }}</span>
          </div>
        }
      </div>
    `,
  styles: [`
    .location-selector {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .full-width {
      width: 100%;
    }

    .location-option {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .location-name {
      font-weight: 500;
      font-size: 14px;
    }

    .location-meta {
      font-size: 12px;
      color: rgba(0, 0, 0, 0.6);
    }

    .error-message {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px;
      background-color: #ffebee;
      border-radius: 4px;
      color: #c62828;
      font-size: 14px;
    }

    .error-message mat-icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
    }

    mat-spinner {
      margin-right: 8px;
    }
  `]
})
export class LocationSelectorComponent implements OnInit, ControlValueAccessor {
  private locationService = inject(LocationService);

  // Input properties
  @Input() required = false;
  @Input() placeholder = 'Select a location';
  @Output() locationSelected = new EventEmitter<Location | null>();

  // Constants
  districts = MAURITIUS_DISTRICTS;

  // Signals for reactive state
  loading = signal<boolean>(false);
  errorMessage = signal<string>('');
  filteredLocations = signal<Location[]>([]);
  isDisabled = signal<boolean>(false);

  // Component state
  selectedDistrict: string | null = null;
  searchTerm = '';
  selectedLocation: Location | null = null;

  // ControlValueAccessor implementation
  private onChange: (value: string | null) => void = () => {};
  private onTouched: () => void = () => {};

  ngOnInit(): void {
    // Load initial locations if a district is pre-selected
    if (this.selectedDistrict) {
      this.loadLocationsByDistrict(this.selectedDistrict);
    }
  }

  /**
   * Handle district selection change
   */
  onDistrictChange(district: string | null): void {
    this.selectedDistrict = district;
    this.searchTerm = '';
    this.filteredLocations.set([]);

    if (district) {
      this.loadLocationsByDistrict(district);
    }
  }

  /**
   * Handle search input change with debounce
   */
  onSearchChange(term: string): void {
    this.errorMessage.set('');

    if (!term || term.length < 2) {
      this.filteredLocations.set([]);
      return;
    }

    this.loading.set(true);

    // Debounce search
    setTimeout(() => {
      this.searchLocations(term);
    }, 300);
  }

  /**
   * Handle location selection from autocomplete
   */
  onLocationSelected(location: Location): void {
    this.selectedLocation = location;
    this.searchTerm = location.name;
    this.onChange(location.id);
    this.onTouched();
    this.locationSelected.emit(location);
  }

  /**
   * Load locations by district
   */
  private loadLocationsByDistrict(district: string): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.locationService.getLocationsByDistrict(district).subscribe({
      next: (locations) => {
        this.filteredLocations.set(locations);
        this.loading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.message);
        this.loading.set(false);
      }
    });
  }

  /**
   * Search locations by name
   */
  private searchLocations(query: string): void {
    this.locationService.searchLocations(query).subscribe({
      next: (locations) => {
        // Filter by district if selected
        const filtered = this.selectedDistrict
          ? locations.filter(loc => loc.district === this.selectedDistrict)
          : locations;

        this.filteredLocations.set(filtered);
        this.loading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.message);
        this.loading.set(false);
      }
    });
  }

  /**
   * Display function for autocomplete
   */
  displayLocation = (location: Location | null): string => {
    return location ? location.name : '';
  };

  // ========================================
  // ControlValueAccessor Implementation
  // ========================================

  writeValue(value: string | null): void {
    if (value) {
      // Load the location by ID
      this.locationService.getLocationById(value).subscribe({
        next: (location) => {
          this.selectedLocation = location;
          this.searchTerm = location.name;
          this.selectedDistrict = location.district;
        },
        error: (error) => {
          this.errorMessage.set('Failed to load location');
          console.error('Error loading location:', error);
        }
      });
    } else {
      this.selectedLocation = null;
      this.searchTerm = '';
    }
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled.set(isDisabled);
  }
}
