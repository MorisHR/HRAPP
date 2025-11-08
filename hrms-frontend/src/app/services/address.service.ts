import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DistrictDto, VillageDto, PostalCodeDto } from '../models/address.models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AddressService {
  private apiUrl = `${environment.apiUrl}/address-lookup`;

  constructor(private http: HttpClient) {}

  /**
   * Get all districts for dropdown population
   */
  getDistricts(): Observable<DistrictDto[]> {
    return this.http.get<DistrictDto[]>(`${this.apiUrl}/districts`);
  }

  /**
   * Get villages by district ID for cascading dropdown
   */
  getVillagesByDistrict(districtId: number): Observable<VillageDto[]> {
    return this.http.get<VillageDto[]>(`${this.apiUrl}/districts/${districtId}/villages`);
  }

  /**
   * Get all villages for dropdown population
   */
  getVillages(): Observable<VillageDto[]> {
    return this.http.get<VillageDto[]>(`${this.apiUrl}/villages`);
  }

  /**
   * Search postal codes by code for autocomplete
   */
  searchPostalCodes(code: string): Observable<PostalCodeDto[]> {
    return this.http.get<PostalCodeDto[]>(`${this.apiUrl}/postal-codes/search?code=${code}`);
  }

  /**
   * Get all postal codes
   */
  getPostalCodes(): Observable<PostalCodeDto[]> {
    return this.http.get<PostalCodeDto[]>(`${this.apiUrl}/postal-codes`);
  }

  /**
   * Lookup postal code and auto-fill address fields
   */
  lookupPostalCode(code: string): Observable<PostalCodeDto[]> {
    return this.searchPostalCodes(code);
  }
}
