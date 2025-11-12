import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Location,
  LocationFilter,
  CreateLocationRequest,
  UpdateLocationRequest,
  LocationResponse,
  SeedLocationsResponse
} from '../models/location';

/**
 * Location Service
 * Manages location data for Mauritius geography (districts, cities, towns, villages)
 *
 * API Endpoints:
 * - GET /api/location - Get all locations with optional filtering
 * - GET /api/location/{id} - Get location by ID
 * - GET /api/location/districts - Get list of all districts
 * - GET /api/location/district/{district} - Get locations by district
 * - GET /api/location/search?q={query} - Search locations by name
 * - POST /api/location - Create new location
 * - PUT /api/location/{id} - Update location
 * - DELETE /api/location/{id} - Delete location
 * - POST /api/location/seed - Seed Mauritius locations (one-time)
 */
@Injectable({
  providedIn: 'root'
})
export class LocationService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/location`;

  /**
   * Get all locations with optional filtering
   * @param filter Optional filter criteria (district, type, searchTerm, isActive)
   * @returns Observable of locations array
   */
  getLocations(filter?: LocationFilter): Observable<Location[]> {
    let params = new HttpParams();

    if (filter) {
      if (filter.district) {
        params = params.set('district', filter.district);
      }
      if (filter.type) {
        params = params.set('type', filter.type);
      }
      if (filter.searchTerm) {
        params = params.set('searchTerm', filter.searchTerm);
      }
      if (filter.isActive !== undefined) {
        params = params.set('isActive', filter.isActive.toString());
      }
      if (filter.region) {
        params = params.set('region', filter.region);
      }
    }

    return this.http.get<LocationResponse>(`${this.apiUrl}`, { params }).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to fetch locations');
        }
        return Array.isArray(response.data) ? response.data : [response.data];
      }),
      catchError(error => {
        console.error('Error fetching locations:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to fetch locations'));
      })
    );
  }

  /**
   * Get location by ID
   * @param id Location ID
   * @returns Observable of location
   */
  getLocationById(id: string): Observable<Location> {
    return this.http.get<LocationResponse>(`${this.apiUrl}/${id}`).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to fetch location');
        }
        return Array.isArray(response.data) ? response.data[0] : response.data;
      }),
      catchError(error => {
        console.error('Error fetching location:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to fetch location'));
      })
    );
  }

  /**
   * Get list of all districts in Mauritius
   * @returns Observable of districts array
   */
  getDistricts(): Observable<string[]> {
    return this.http.get<{ success: boolean; data: string[]; message?: string }>(`${this.apiUrl}/districts`).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to fetch districts');
        }
        return response.data;
      }),
      catchError(error => {
        console.error('Error fetching districts:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to fetch districts'));
      })
    );
  }

  /**
   * Get all locations in a specific district
   * @param district District name
   * @returns Observable of locations array
   */
  getLocationsByDistrict(district: string): Observable<Location[]> {
    return this.http.get<LocationResponse>(`${this.apiUrl}/district/${encodeURIComponent(district)}`).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to fetch locations');
        }
        return Array.isArray(response.data) ? response.data : [response.data];
      }),
      catchError(error => {
        console.error('Error fetching locations by district:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to fetch locations'));
      })
    );
  }

  /**
   * Search locations by name
   * @param query Search query
   * @returns Observable of matching locations
   */
  searchLocations(query: string): Observable<Location[]> {
    const params = new HttpParams().set('q', query);

    return this.http.get<LocationResponse>(`${this.apiUrl}/search`, { params }).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to search locations');
        }
        return Array.isArray(response.data) ? response.data : [response.data];
      }),
      catchError(error => {
        console.error('Error searching locations:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to search locations'));
      })
    );
  }

  /**
   * Create a new location
   * @param location Location data
   * @returns Observable of created location
   */
  createLocation(location: CreateLocationRequest): Observable<Location> {
    return this.http.post<LocationResponse>(this.apiUrl, location).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to create location');
        }
        return Array.isArray(response.data) ? response.data[0] : response.data;
      }),
      catchError(error => {
        console.error('Error creating location:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to create location'));
      })
    );
  }

  /**
   * Update an existing location
   * @param id Location ID
   * @param location Updated location data
   * @returns Observable of updated location
   */
  updateLocation(id: string, location: UpdateLocationRequest): Observable<Location> {
    return this.http.put<LocationResponse>(`${this.apiUrl}/${id}`, location).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to update location');
        }
        return Array.isArray(response.data) ? response.data[0] : response.data;
      }),
      catchError(error => {
        console.error('Error updating location:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to update location'));
      })
    );
  }

  /**
   * Delete a location
   * @param id Location ID
   * @returns Observable of void
   */
  deleteLocation(id: string): Observable<void> {
    return this.http.delete<{ success: boolean; message?: string }>(`${this.apiUrl}/${id}`).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to delete location');
        }
        return;
      }),
      catchError(error => {
        console.error('Error deleting location:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to delete location'));
      })
    );
  }

  /**
   * Seed Mauritius locations (one-time operation)
   * Populates the database with all Mauritius districts, cities, towns, and villages
   * @returns Observable of seed result
   */
  seedMauritiusLocations(): Observable<SeedLocationsResponse> {
    return this.http.post<SeedLocationsResponse>(`${this.apiUrl}/seed`, {}).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to seed locations');
        }
        return response;
      }),
      catchError(error => {
        console.error('Error seeding locations:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to seed locations'));
      })
    );
  }
}
