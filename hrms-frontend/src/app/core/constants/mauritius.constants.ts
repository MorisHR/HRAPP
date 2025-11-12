/**
 * Mauritius-specific constants for the HRMS application
 * Contains all 9 districts of Mauritius and location type definitions
 */

/**
 * The 9 administrative districts of Mauritius
 * Used for location categorization and filtering
 */
export const MAURITIUS_DISTRICTS = [
  'Port Louis',
  'Pamplemousses',
  'Rivière du Rempart',
  'Flacq',
  'Grand Port',
  'Savanne',
  'Plaines Wilhems',
  'Moka',
  'Black River'
] as const;

/**
 * Location types with display labels
 * Used in forms and filters
 */
export const LOCATION_TYPES = [
  { value: 'City', label: 'City' },
  { value: 'Town', label: 'Town' },
  { value: 'Village', label: 'Village' },
  { value: 'Suburb', label: 'Suburb' },
  { value: 'Other', label: 'Other' }
] as const;

/**
 * Type-safe district type
 */
export type MauritiusDistrict = typeof MAURITIUS_DISTRICTS[number];
