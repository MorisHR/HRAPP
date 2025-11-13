import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

/**
 * Device API Key DTO
 * Represents an API key associated with a biometric device
 */
export interface DeviceApiKeyDto {
  id: string;
  description: string;
  isActive: boolean;
  expiresAt?: string;
  lastUsedAt?: string;
  usageCount: number;
  createdAt: string;
  daysUntilExpiration?: number;
}

/**
 * Response when generating a new API key
 * Contains the plaintext API key (only shown once)
 */
export interface GenerateApiKeyResponse {
  apiKeyId: string;
  plaintextKey: string;  // Plaintext, only shown once
  description: string;
  expiresAt?: string;
  isActive: boolean;
  createdAt: string;
  rateLimitPerMinute: number;
  securityWarning: string;
}

/**
 * Request DTO for generating a new API key
 */
export interface GenerateApiKeyRequest {
  description: string;
}

/**
 * Device API Key Service
 * Handles all API key management operations for biometric devices
 */
@Injectable({
  providedIn: 'root'
})
export class DeviceApiKeyService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/biometric-devices`;

  /**
   * Get all API keys for a specific device
   * @param deviceId The device ID
   * @returns Observable of API key array
   */
  getDeviceApiKeys(deviceId: string): Observable<DeviceApiKeyDto[]> {
    return this.http.get<{ success: boolean; data: DeviceApiKeyDto[] }>(
      `${this.apiUrl}/${deviceId}/api-keys`
    ).pipe(map(response => response.data));
  }

  /**
   * Generate a new API key for a device
   * @param deviceId The device ID
   * @param description Description of the API key's purpose
   * @returns Observable with the generated API key (plaintext)
   */
  generateApiKey(deviceId: string, description: string): Observable<GenerateApiKeyResponse> {
    const request: GenerateApiKeyRequest = { description };
    return this.http.post<{ success: boolean; data: GenerateApiKeyResponse }>(
      `${this.apiUrl}/${deviceId}/generate-api-key`,
      request
    ).pipe(map(response => response.data));
  }

  /**
   * Revoke an API key
   * @param deviceId The device ID
   * @param apiKeyId The API key ID to revoke
   * @returns Observable
   */
  revokeApiKey(deviceId: string, apiKeyId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${deviceId}/api-keys/${apiKeyId}`
    );
  }

  /**
   * Rotate an API key (revoke old and generate new)
   * @param deviceId The device ID
   * @param apiKeyId The API key ID to rotate
   * @returns Observable with the new API key (plaintext)
   */
  rotateApiKey(deviceId: string, apiKeyId: string): Observable<GenerateApiKeyResponse> {
    return this.http.post<{ success: boolean; data: GenerateApiKeyResponse }>(
      `${this.apiUrl}/${deviceId}/api-keys/${apiKeyId}/rotate`,
      {}
    ).pipe(map(response => response.data));
  }
}
