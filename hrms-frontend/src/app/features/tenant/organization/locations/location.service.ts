import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';

/**
 * Complete location information for detail view
 * Matches backend LocationDto
 */
export interface LocationDto {
  id: string;
  locationCode: string;
  locationName: string;
  locationType?: string;

  // Address
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  region?: string;
  postalCode?: string;
  country: string;

  // Contact
  phone?: string;
  email?: string;

  // Working Hours
  workingHoursJson?: string;
  timezone: string;

  // Management
  locationManagerId?: string;
  locationManagerName?: string;
  capacityHeadcount?: number;

  // Geographic coordinates
  latitude?: number;
  longitude?: number;

  // Status
  isActive: boolean;

  // Statistics
  deviceCount: number;
  employeeCount: number;

  // Audit
  createdAt: string;
  updatedAt?: string;
  createdBy?: string;
  updatedBy?: string;
}

/**
 * DTO for creating a new location
 * Matches backend CreateLocationDto
 */
export interface CreateLocationDto {
  locationCode: string;
  locationName: string;
  locationType?: string;

  // Address
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  region?: string;
  postalCode?: string;
  country: string;

  // Contact
  phone?: string;
  email?: string;

  // Working Hours (JSON string)
  workingHoursJson?: string;
  timezone: string;

  // Management
  locationManagerId?: string;
  capacityHeadcount?: number;

  // Geographic coordinates
  latitude?: number;
  longitude?: number;

  isActive: boolean;
}

/**
 * DTO for updating an existing location
 * Matches backend UpdateLocationDto
 */
export interface UpdateLocationDto {
  locationCode: string;
  locationName: string;
  locationType?: string;

  // Address
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  region?: string;
  postalCode?: string;
  country: string;

  // Contact
  phone?: string;
  email?: string;

  // Working Hours (JSON string)
  workingHoursJson?: string;
  timezone: string;

  // Management
  locationManagerId?: string;
  capacityHeadcount?: number;

  // Geographic coordinates
  latitude?: number;
  longitude?: number;

  isActive: boolean;
}

/**
 * Simplified location DTO for dropdowns
 */
export interface LocationDropdownDto {
  id: string;
  locationCode: string;
  locationName: string;
  locationType?: string;
  isActive: boolean;
}

/**
 * Location summary for list views
 */
export interface LocationSummaryDto {
  id: string;
  locationCode: string;
  locationName: string;
  locationType?: string;
  city?: string;
  region?: string;
  country: string;
  deviceCount: number;
  employeeCount: number;
  isActive: boolean;
}

/**
 * Location Service
 * Handles all API interactions for location management
 */
@Injectable({
  providedIn: 'root'
})
export class LocationService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/locations`;

  /**
   * Get all locations
   */
  getAll(): Observable<LocationDto[]> {
    return this.http.get<{ success: boolean; data: LocationDto[] }>(this.apiUrl)
      .pipe(map(response => response.data));
  }

  /**
   * Get a single location by ID
   */
  getById(id: string): Observable<LocationDto> {
    return this.http.get<{ success: boolean; data: LocationDto }>(`${this.apiUrl}/${id}`)
      .pipe(map(response => response.data));
  }

  /**
   * Create a new location
   */
  create(location: CreateLocationDto): Observable<any> {
    return this.http.post(this.apiUrl, location);
  }

  /**
   * Update an existing location
   */
  update(id: string, location: UpdateLocationDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, location);
  }

  /**
   * Delete a location (soft delete)
   */
  delete(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  /**
   * Get location summaries for list view
   */
  getSummaries(): Observable<LocationSummaryDto[]> {
    return this.http.get<{ success: boolean; data: LocationSummaryDto[] }>(`${this.apiUrl}/summaries`)
      .pipe(map(response => response.data));
  }

  /**
   * Get simple dropdown list of active locations
   */
  getDropdown(): Observable<LocationDropdownDto[]> {
    return this.http.get<{ success: boolean; data: LocationDropdownDto[] }>(`${this.apiUrl}/dropdown`)
      .pipe(map(response => response.data));
  }

  /**
   * Get locations by type
   */
  getByType(locationType: string): Observable<LocationDto[]> {
    return this.http.get<{ success: boolean; data: LocationDto[] }>(`${this.apiUrl}/by-type/${locationType}`)
      .pipe(map(response => response.data));
  }

  /**
   * Activate a location
   */
  activate(id: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${id}/activate`, {});
  }

  /**
   * Deactivate a location
   */
  deactivate(id: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${id}/deactivate`, {});
  }

  /**
   * Get statistics for a location
   */
  getStatistics(id: string): Observable<{
    deviceCount: number;
    employeeCount: number;
    activeDeviceCount: number;
    lastSyncTime?: string;
  }> {
    return this.http.get<{ success: boolean; data: any }>(`${this.apiUrl}/${id}/statistics`)
      .pipe(map(response => response.data));
  }
}
