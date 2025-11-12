/**
 * Location models for Mauritius geography management
 * Supports the 9 districts of Mauritius with cities, towns, villages, and suburbs
 */

/**
 * Location entity representing a place in Mauritius
 */
export interface Location {
  id: string;
  district: string;
  type: LocationType;
  name: string;
  region?: string;
  postalCode?: string;
  latitude?: number;
  longitude?: number;
  isActive: boolean;
  createdAt?: Date;
  updatedAt?: Date;
}

/**
 * Location type enumeration
 */
export enum LocationType {
  City = 'City',
  Town = 'Town',
  Village = 'Village',
  Suburb = 'Suburb',
  Other = 'Other'
}

/**
 * Filter criteria for location queries
 */
export interface LocationFilter {
  district?: string;
  type?: LocationType;
  searchTerm?: string;
  isActive?: boolean;
  region?: string;
}

/**
 * Request payload for creating a new location
 */
export interface CreateLocationRequest {
  district: string;
  type: LocationType;
  name: string;
  region?: string;
  postalCode?: string;
  latitude?: number;
  longitude?: number;
  isActive?: boolean;
}

/**
 * Request payload for updating an existing location
 */
export interface UpdateLocationRequest {
  district?: string;
  type?: LocationType;
  name?: string;
  region?: string;
  postalCode?: string;
  latitude?: number;
  longitude?: number;
  isActive?: boolean;
}

/**
 * API response wrapper for location data
 */
export interface LocationResponse {
  success: boolean;
  data: Location | Location[];
  message?: string;
}

/**
 * Response for seed operation
 */
export interface SeedLocationsResponse {
  success: boolean;
  data: {
    totalSeeded: number;
    districts: string[];
    locations: Location[];
  };
  message: string;
}
