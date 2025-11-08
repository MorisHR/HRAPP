export interface DistrictDto {
  id: number;
  districtCode: string;
  districtName: string;
  districtNameFrench?: string;
  region: string;
  areaSqKm?: number;
  population?: number;
  displayOrder: number;
  isActive: boolean;
}

export interface VillageDto {
  id: number;
  villageCode: string;
  villageName: string;
  postalCode: string;
  districtId: number;
  displayOrder: number;
  isActive: boolean;
}

export interface PostalCodeDto {
  id: number;
  code: string;
  villageName: string;
  districtName: string;
  region: string;
  villageId: number;
  districtId: number;
  isPrimary: boolean;
  isActive: boolean;
}
