/**
 * FORTUNE 500 PATTERN: Industry Sector Model
 * Matches backend IndustrySectorDto exactly for type safety
 * Used for: Dropdown options, tenant sector display
 */
export interface IndustrySector {
  id: number;
  code: string;
  name: string;
  nameFrench?: string;
  isActive: boolean;
}

/**
 * API Response wrapper
 */
export interface IndustrySectorsResponse {
  success: boolean;
  data: IndustrySector[];
  count: number;
}
