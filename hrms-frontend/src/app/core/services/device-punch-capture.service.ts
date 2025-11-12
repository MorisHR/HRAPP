import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DevicePunchCaptureDto {
  deviceSerialNumber: string;
  deviceUserId: string;
  punchTime: string; // ISO 8601 format
  punchType: 'CheckIn' | 'CheckOut' | 'BreakStart' | 'BreakEnd';
  verificationMethod: 'Fingerprint' | 'Face' | 'Card' | 'PIN' | 'Password';
  verificationQuality?: number;
  latitude?: number;
  longitude?: number;
  photo?: string; // Base64 encoded
  temperature?: number;
  maskDetected?: boolean;
}

export interface PunchProcessingResultDto {
  success: boolean;
  message: string;
  punchRecordId?: string;
  attendanceId?: string;
  warnings: string[];
  errors: string[];
  hasWarnings: boolean;
  hasErrors: boolean;
}

export interface DeviceHealthDto {
  status: string;
  serverTime: string;
  version: string;
  message: string;
}

export interface DeviceSyncStatusDto {
  deviceId: string;
  deviceCode: string;
  machineName: string;
  syncEnabled: boolean;
  lastSyncTime?: string;
  minutesSinceLastSync?: number;
  isOnline: boolean;
}

export interface PunchHistoryDto {
  id: string;
  deviceUserId: string;
  punchTime: string;
  punchType: string;
  processingStatus: string;
  employeeName?: string;
  employeeId?: string;
  attendanceId?: string;
  warnings?: string[];
  errors?: string[];
}

export interface PaginatedPunchHistoryDto {
  success: boolean;
  data: PunchHistoryDto[];
  pagination: {
    currentPage: number;
    pageSize: number;
    totalRecords: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
  };
  filters: {
    hours: number;
    startDate: string;
    endDate: string;
  };
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class DevicePunchCaptureService {
  private apiUrl = `${environment.apiUrl}/device`;

  constructor(private http: HttpClient) {}

  /**
   * Capture biometric punch from device
   * POST /api/device/capture-punch
   *
   * NOTE: This endpoint requires X-Device-API-Key header
   * It's typically called by biometric devices, not frontend
   */
  capturePunch(dto: DevicePunchCaptureDto, apiKey: string): Observable<PunchProcessingResultDto> {
    const headers = { 'X-Device-API-Key': apiKey };
    return this.http.post<PunchProcessingResultDto>(`${this.apiUrl}/capture-punch`, dto, { headers });
  }

  /**
   * Health check endpoint for devices
   * GET /api/device/health
   *
   * NO AUTHENTICATION REQUIRED - Public endpoint
   */
  getHealth(): Observable<DeviceHealthDto> {
    return this.http.get<DeviceHealthDto>(`${this.apiUrl}/health`);
  }

  /**
   * Get sync status for authenticated device
   * GET /api/device/sync-status
   *
   * NOTE: Requires X-Device-API-Key header
   */
  getSyncStatus(apiKey: string): Observable<{ success: boolean; data: DeviceSyncStatusDto; serverTime: string }> {
    const headers = { 'X-Device-API-Key': apiKey };
    return this.http.get<{ success: boolean; data: DeviceSyncStatusDto; serverTime: string }>(
      `${this.apiUrl}/sync-status`,
      { headers }
    );
  }

  /**
   * Get recent punch history for authenticated device
   * GET /api/device/punch-history
   *
   * NOTE: Requires X-Device-API-Key header
   * @param apiKey Device API key
   * @param hours Number of hours to look back (default: 24, max: 168)
   * @param page Page number (default: 1)
   * @param pageSize Page size (default: 50)
   */
  getPunchHistory(
    apiKey: string,
    hours: number = 24,
    page: number = 1,
    pageSize: number = 50
  ): Observable<PaginatedPunchHistoryDto> {
    const headers = { 'X-Device-API-Key': apiKey };
    const params = new HttpParams()
      .set('hours', hours.toString())
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PaginatedPunchHistoryDto>(`${this.apiUrl}/punch-history`, { headers, params });
  }
}
