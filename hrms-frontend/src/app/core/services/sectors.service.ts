import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SectorDto {
  id: number;
  code: string;
  name: string;
  parentSectorId?: number;
  parentSectorName?: string;
  description?: string;
  level: number;
  requiresSpecialPermit: boolean;
  isActive: boolean;
  createdAt: string;
  subSectors?: SectorDto[];
}

export interface SectorComplianceRuleDto {
  id: number;
  sectorId: number;
  category: string;
  ruleName: string;
  description: string;
  isActive: boolean;
  configuration?: any;
}

export interface CreateSectorDto {
  code: string;
  name: string;
  parentSectorId?: number;
  description?: string;
  requiresSpecialPermit?: boolean;
}

export interface UpdateSectorDto {
  name?: string;
  description?: string;
  requiresSpecialPermit?: boolean;
  isActive?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class SectorsService {
  private apiUrl = `${environment.apiUrl}/Sectors`;

  constructor(private http: HttpClient) {}

  /**
   * Get all sectors with hierarchical structure
   * GET /api/Sectors/hierarchical
   */
  getAllSectorsHierarchical(): Observable<{ success: boolean; data: SectorDto[]; count: number }> {
    return this.http.get<{ success: boolean; data: SectorDto[]; count: number }>(`${this.apiUrl}/hierarchical`);
  }

  /**
   * Get all sectors as flat list
   * GET /api/Sectors
   */
  getAllSectors(activeOnly: boolean = true): Observable<{ success: boolean; data: SectorDto[]; count: number }> {
    const params = new HttpParams().set('activeOnly', activeOnly.toString());
    return this.http.get<{ success: boolean; data: SectorDto[]; count: number }>(this.apiUrl, { params });
  }

  /**
   * Get sector by ID
   * GET /api/Sectors/{id}
   */
  getSectorById(id: number): Observable<{ success: boolean; data: SectorDto }> {
    return this.http.get<{ success: boolean; data: SectorDto }>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get sector by code
   * GET /api/Sectors/code/{code}
   */
  getSectorByCode(code: string): Observable<{ success: boolean; data: SectorDto }> {
    return this.http.get<{ success: boolean; data: SectorDto }>(`${this.apiUrl}/code/${code}`);
  }

  /**
   * Get compliance rules for a sector
   * GET /api/Sectors/{id}/compliance-rules
   */
  getComplianceRulesForSector(id: number): Observable<{ success: boolean; data: SectorComplianceRuleDto[]; count: number }> {
    return this.http.get<{ success: boolean; data: SectorComplianceRuleDto[]; count: number }>(`${this.apiUrl}/${id}/compliance-rules`);
  }

  /**
   * Get compliance rule by category for a sector
   * GET /api/Sectors/{id}/compliance-rules/{category}
   */
  getComplianceRuleByCategory(id: number, category: string): Observable<{ success: boolean; data: SectorComplianceRuleDto }> {
    return this.http.get<{ success: boolean; data: SectorComplianceRuleDto }>(`${this.apiUrl}/${id}/compliance-rules/${category}`);
  }

  /**
   * Get parent sectors (top-level sectors)
   * GET /api/Sectors/parents
   */
  getParentSectors(): Observable<{ success: boolean; data: SectorDto[]; count: number }> {
    return this.http.get<{ success: boolean; data: SectorDto[]; count: number }>(`${this.apiUrl}/parents`);
  }

  /**
   * Get sub-sectors for a parent sector
   * GET /api/Sectors/{id}/sub-sectors
   */
  getSubSectors(id: number): Observable<{ success: boolean; data: SectorDto[]; count: number }> {
    return this.http.get<{ success: boolean; data: SectorDto[]; count: number }>(`${this.apiUrl}/${id}/sub-sectors`);
  }

  /**
   * Search sectors by name or code
   * GET /api/Sectors/search
   */
  searchSectors(query: string): Observable<{ success: boolean; data: SectorDto[]; count: number }> {
    const params = new HttpParams().set('query', query);
    return this.http.get<{ success: boolean; data: SectorDto[]; count: number }>(`${this.apiUrl}/search`, { params });
  }

  /**
   * Get sectors requiring special permits
   * GET /api/Sectors/requiring-permits
   */
  getSectorsRequiringPermits(): Observable<{ success: boolean; data: SectorDto[]; count: number }> {
    return this.http.get<{ success: boolean; data: SectorDto[]; count: number }>(`${this.apiUrl}/requiring-permits`);
  }

  /**
   * Create new industry sector (Super Admin only)
   * POST /api/Sectors
   */
  createSector(dto: CreateSectorDto): Observable<{ success: boolean; data: SectorDto; message: string }> {
    return this.http.post<{ success: boolean; data: SectorDto; message: string }>(this.apiUrl, dto);
  }

  /**
   * Update existing sector (Super Admin only)
   * PUT /api/Sectors/{id}
   */
  updateSector(id: number, dto: UpdateSectorDto): Observable<{ success: boolean; data: SectorDto; message: string }> {
    return this.http.put<{ success: boolean; data: SectorDto; message: string }>(`${this.apiUrl}/${id}`, dto);
  }
}
