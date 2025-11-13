import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';

/**
 * Complete biometric device information for detail view
 * Matches backend BiometricDeviceDto
 */
export interface BiometricDeviceDto {
  id: string;
  deviceCode: string;
  machineName: string;
  machineId: string;
  deviceType: string;
  model?: string;
  locationId?: string;
  locationName?: string;
  locationCode?: string;
  departmentId?: string;
  departmentName?: string;
  ipAddress?: string;
  port: number;
  macAddress?: string;
  serialNumber?: string;
  firmwareVersion?: string;
  syncEnabled: boolean;
  syncIntervalMinutes: number;
  lastSyncTime?: string;
  lastSyncStatus?: string;
  lastSyncRecordCount: number;
  connectionMethod: string;
  connectionTimeoutSeconds: number;
  deviceStatus: string;
  isActive: boolean;
  offlineAlertEnabled: boolean;
  offlineThresholdMinutes: number;
  totalAttendanceRecords: number;
  authorizedEmployeeCount: number;
  createdAt: string;
  updatedAt?: string;
  createdBy?: string;
  updatedBy?: string;
}

/**
 * DTO for creating a new biometric device
 * Matches backend CreateBiometricDeviceDto
 */
export interface CreateBiometricDeviceDto {
  deviceCode: string;
  machineName: string;
  machineId?: string;
  deviceType: string;
  model?: string;
  locationId?: string;
  departmentId?: string;
  ipAddress?: string;
  port: number;
  macAddress?: string;
  serialNumber?: string;
  firmwareVersion?: string;
  syncEnabled: boolean;
  syncIntervalMinutes: number;
  connectionMethod: string;
  connectionTimeoutSeconds: number;
  offlineAlertEnabled: boolean;
  offlineThresholdMinutes: number;
  deviceConfigJson?: string;
  isActive: boolean;
}

/**
 * DTO for updating an existing biometric device
 * Matches backend UpdateBiometricDeviceDto
 */
export interface UpdateBiometricDeviceDto {
  deviceCode: string;
  machineName: string;
  machineId?: string;
  deviceType: string;
  model?: string;
  locationId?: string;
  departmentId?: string;
  ipAddress?: string;
  port: number;
  macAddress?: string;
  serialNumber?: string;
  firmwareVersion?: string;
  syncEnabled: boolean;
  syncIntervalMinutes: number;
  connectionMethod: string;
  connectionTimeoutSeconds: number;
  offlineAlertEnabled: boolean;
  offlineThresholdMinutes: number;
  deviceConfigJson?: string;
  isActive: boolean;
}

/**
 * DTO for testing device connection
 */
export interface TestConnectionDto {
  ipAddress: string;
  port: number;
  connectionMethod: string;
  connectionTimeoutSeconds?: number;
  username?: string;
  password?: string;
}

/**
 * Result of connection test
 * Matches backend ConnectionTestResultDto
 */
export interface ConnectionTestResult {
  success: boolean;
  message: string;
  responseTimeMs: number;
  deviceInfo?: string;
  firmwareVersion?: string;
  recordsAvailable?: number;
  errorDetails?: string;
  diagnostics?: string;
  testedAt: string;
}

/**
 * Result of manual sync trigger
 * Matches backend ManualSyncResultDto
 */
export interface ManualSyncResult {
  success: boolean;
  message: string;
  jobId?: string;
  deviceId: string;
  deviceName: string;
  queuedAt: string;
  estimatedDurationSeconds?: number;
  errorDetails?: string;
  syncAlreadyInProgress: boolean;
}

/**
 * Device sync status information
 * Matches backend DeviceSyncStatusDto
 */
export interface DeviceSyncStatusDto {
  deviceId: string;
  deviceCode: string;
  machineName: string;
  locationName?: string;
  syncEnabled: boolean;
  syncIntervalMinutes: number;
  lastSyncTime?: string;
  lastSyncStatus?: string;
  lastSyncRecordCount: number;
  minutesSinceLastSync?: number;
  deviceStatus: string;
  isOnline: boolean;
  isOfflineAlertTriggered: boolean;
  totalSyncCount: number;
  successfulSyncCount: number;
  failedSyncCount: number;
  syncSuccessRate: number;
}

/**
 * Biometric Device Service
 * Handles all API interactions for biometric device management
 */
@Injectable({
  providedIn: 'root'
})
export class BiometricDeviceService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/biometric-devices`;

  /**
   * Get all biometric devices
   */
  getDevices(): Observable<BiometricDeviceDto[]> {
    return this.http.get<{ success: boolean; data: BiometricDeviceDto[] }>(this.apiUrl)
      .pipe(map(response => response.data));
  }

  /**
   * Get a single biometric device by ID
   */
  getDevice(id: string): Observable<BiometricDeviceDto> {
    return this.http.get<{ success: boolean; data: BiometricDeviceDto }>(`${this.apiUrl}/${id}`)
      .pipe(map(response => response.data));
  }

  /**
   * Create a new biometric device
   */
  createDevice(device: CreateBiometricDeviceDto): Observable<BiometricDeviceDto> {
    return this.http.post<{ success: boolean; data: BiometricDeviceDto }>(this.apiUrl, device)
      .pipe(map(response => response.data));
  }

  /**
   * Update an existing biometric device
   */
  updateDevice(id: string, device: UpdateBiometricDeviceDto): Observable<BiometricDeviceDto> {
    return this.http.put<{ success: boolean; data: BiometricDeviceDto }>(`${this.apiUrl}/${id}`, device)
      .pipe(map(response => response.data));
  }

  /**
   * Delete a biometric device
   */
  deleteDevice(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Test connection to a biometric device
   */
  testConnection(connectionData: TestConnectionDto): Observable<ConnectionTestResult> {
    return this.http.post<{ success: boolean; data: ConnectionTestResult }>(
      `${this.apiUrl}/test-connection`,
      connectionData
    ).pipe(map(response => response.data));
  }

  /**
   * Manually trigger device sync
   */
  syncDevice(id: string): Observable<ManualSyncResult> {
    return this.http.post<{ success: boolean; data: ManualSyncResult }>(
      `${this.apiUrl}/${id}/sync`,
      {}
    ).pipe(map(response => response.data));
  }

  /**
   * Alias for syncDevice - Manually trigger device sync
   */
  triggerSync(id: string): Observable<ManualSyncResult> {
    return this.syncDevice(id);
  }

  /**
   * Get device sync status
   */
  getSyncStatus(id: string): Observable<DeviceSyncStatusDto> {
    return this.http.get<{ success: boolean; data: DeviceSyncStatusDto }>(
      `${this.apiUrl}/${id}/sync-status`
    ).pipe(map(response => response.data));
  }

  /**
   * Get all device sync statuses
   */
  getAllSyncStatuses(): Observable<DeviceSyncStatusDto[]> {
    return this.http.get<{ success: boolean; data: DeviceSyncStatusDto[] }>(
      `${this.apiUrl}/sync-statuses`
    ).pipe(map(response => response.data));
  }

  /**
   * Get devices by location
   */
  getDevicesByLocation(locationId: string): Observable<BiometricDeviceDto[]> {
    return this.http.get<{ success: boolean; data: BiometricDeviceDto[] }>(
      `${this.apiUrl}/by-location/${locationId}`
    ).pipe(map(response => response.data));
  }

  /**
   * Get devices by type
   */
  getDevicesByType(deviceType: string): Observable<BiometricDeviceDto[]> {
    return this.http.get<{ success: boolean; data: BiometricDeviceDto[] }>(
      `${this.apiUrl}/by-type/${deviceType}`
    ).pipe(map(response => response.data));
  }

  /**
   * Activate a device
   */
  activateDevice(id: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/activate`, {});
  }

  /**
   * Deactivate a device
   */
  deactivateDevice(id: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  /**
   * Enable sync for a device
   */
  enableSync(id: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/enable-sync`, {});
  }

  /**
   * Disable sync for a device
   */
  disableSync(id: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/disable-sync`, {});
  }

  /**
   * Get device statistics
   */
  getDeviceStatistics(id: string): Observable<{
    totalAttendanceRecords: number;
    authorizedEmployeeCount: number;
    syncCount: number;
    lastSyncTime?: string;
    averageSyncDuration?: number;
  }> {
    return this.http.get<{ success: boolean; data: any }>(
      `${this.apiUrl}/${id}/statistics`
    ).pipe(map(response => response.data));
  }

  /**
   * Get authorized employees for a device
   */
  getAuthorizedEmployees(deviceId: string): Observable<any[]> {
    return this.http.get<{ success: boolean; data: any[] }>(
      `${this.apiUrl}/${deviceId}/authorized-employees`
    ).pipe(map(response => response.data));
  }

  /**
   * Authorize employee for device access
   */
  authorizeEmployee(deviceId: string, employeeId: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/${deviceId}/authorize-employee/${employeeId}`,
      {}
    );
  }

  /**
   * Revoke employee authorization from device
   */
  revokeEmployeeAuthorization(deviceId: string, employeeId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${deviceId}/revoke-employee/${employeeId}`
    );
  }

  /**
   * Get device sync logs
   */
  getSyncLogs(deviceId: string, limit: number = 50): Observable<any[]> {
    return this.http.get<{ success: boolean; data: any[] }>(
      `${this.apiUrl}/${deviceId}/sync-logs?limit=${limit}`
    ).pipe(map(response => response.data));
  }

  /**
   * Get attendance anomalies detected by device
   */
  getAnomalies(deviceId: string, limit: number = 50): Observable<any[]> {
    return this.http.get<{ success: boolean; data: any[] }>(
      `${this.apiUrl}/${deviceId}/anomalies?limit=${limit}`
    ).pipe(map(response => response.data));
  }

  /**
   * Update device configuration
   */
  updateDeviceConfig(deviceId: string, config: any): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/${deviceId}/config`,
      config
    );
  }

  /**
   * Restart device (if supported)
   */
  restartDevice(deviceId: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/${deviceId}/restart`,
      {}
    );
  }

  /**
   * Clear device data (if supported)
   */
  clearDeviceData(deviceId: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/${deviceId}/clear-data`,
      {}
    );
  }
}
