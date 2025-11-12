import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, interval, Subject, takeUntil, tap, catchError, of, map } from 'rxjs';
import { BiometricDevice, DeviceStatus } from '../models/attendance.model';
import { environment } from '../../../environments/environment';

/**
 * Device status monitoring service
 * Tracks health and status of biometric devices
 * Provides real-time updates on device connectivity
 */
@Injectable({
  providedIn: 'root'
})
export class DeviceStatusService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/devices`;

  // Polling configuration
  private readonly POLLING_INTERVAL = 30000; // 30 seconds
  private pollingActive = false;
  private destroy$ = new Subject<void>();

  // State signals
  private devicesSignal = signal<BiometricDevice[]>([]);
  private loadingSignal = signal<boolean>(false);
  private errorSignal = signal<string | null>(null);
  private lastUpdateSignal = signal<Date | null>(null);

  // Computed signals
  readonly devices = this.devicesSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly lastUpdate = this.lastUpdateSignal.asReadonly();

  readonly onlineDevices = computed(() =>
    this.devicesSignal().filter(d => d.isOnline && d.status === DeviceStatus.Online)
  );

  readonly offlineDevices = computed(() =>
    this.devicesSignal().filter(d => !d.isOnline || d.status === DeviceStatus.Offline)
  );

  readonly errorDevices = computed(() =>
    this.devicesSignal().filter(d => d.status === DeviceStatus.Error)
  );

  readonly maintenanceDevices = computed(() =>
    this.devicesSignal().filter(d => d.status === DeviceStatus.Maintenance)
  );

  readonly totalDevices = computed(() => this.devicesSignal().length);
  readonly onlineCount = computed(() => this.onlineDevices().length);
  readonly offlineCount = computed(() => this.offlineDevices().length);
  readonly healthPercentage = computed(() => {
    const total = this.totalDevices();
    if (total === 0) return 0;
    return Math.round((this.onlineCount() / total) * 100);
  });

  constructor() {
    console.log('🖥️ DeviceStatusService initialized');
  }

  /**
   * Load all devices from API
   */
  loadDevices(): Observable<BiometricDevice[]> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    return this.http.get<any>(`${this.apiUrl}`).pipe(
      map(response => {
        // Handle backend response format: { success, data, message }
        const devices = response.success ? response.data : [];
        this.devicesSignal.set(devices || []);
        this.lastUpdateSignal.set(new Date());
        this.loadingSignal.set(false);
        console.log('✅ Devices loaded:', devices?.length || 0);
        return devices as BiometricDevice[];
      }),
      catchError(error => {
        console.error('❌ Failed to load devices:', error);
        this.errorSignal.set(error.message || 'Failed to load devices');
        this.loadingSignal.set(false);
        return of([] as BiometricDevice[]);
      })
    );
  }

  /**
   * Get device by ID
   */
  getDevice(deviceId: string): Observable<BiometricDevice> {
    return this.http.get<any>(`${this.apiUrl}/${deviceId}`).pipe(
      tap(response => {
        if (response.success && response.data) {
          // Update device in list
          const devices = this.devicesSignal();
          const index = devices.findIndex(d => d.id === deviceId);
          if (index !== -1) {
            devices[index] = response.data;
            this.devicesSignal.set([...devices]);
          }
        }
      }),
      catchError(error => {
        console.error(`❌ Failed to load device ${deviceId}:`, error);
        throw error;
      })
    );
  }

  /**
   * Update device status manually
   */
  updateDeviceStatus(deviceId: string, status: DeviceStatus): Observable<any> {
    return this.http.patch<any>(`${this.apiUrl}/${deviceId}/status`, { status }).pipe(
      tap(response => {
        if (response.success) {
          const devices = this.devicesSignal();
          const index = devices.findIndex(d => d.id === deviceId);
          if (index !== -1) {
            devices[index].status = status;
            this.devicesSignal.set([...devices]);
          }
          console.log(`✅ Device ${deviceId} status updated to ${status}`);
        }
      }),
      catchError(error => {
        console.error(`❌ Failed to update device ${deviceId} status:`, error);
        throw error;
      })
    );
  }

  /**
   * Sync device data from hardware
   */
  syncDevice(deviceId: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${deviceId}/sync`, {}).pipe(
      tap(response => {
        if (response.success) {
          console.log(`✅ Device ${deviceId} synced successfully`);
          // Reload device data
          this.getDevice(deviceId).subscribe();
        }
      }),
      catchError(error => {
        console.error(`❌ Failed to sync device ${deviceId}:`, error);
        throw error;
      })
    );
  }

  /**
   * Test device connectivity
   */
  testDeviceConnection(deviceId: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${deviceId}/test`, {}).pipe(
      tap(response => {
        console.log(`✅ Device ${deviceId} connection test:`, response);
      }),
      catchError(error => {
        console.error(`❌ Device ${deviceId} connection test failed:`, error);
        throw error;
      })
    );
  }

  /**
   * Start automatic polling for device status updates
   */
  startPolling(): void {
    if (this.pollingActive) {
      console.log('⚠️ Polling already active');
      return;
    }

    this.pollingActive = true;
    console.log('▶️ Starting device status polling');

    // Load initial data
    this.loadDevices().subscribe();

    // Poll every 30 seconds
    interval(this.POLLING_INTERVAL)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        if (!this.loadingSignal()) {
          this.loadDevices().subscribe();
        }
      });
  }

  /**
   * Stop automatic polling
   */
  stopPolling(): void {
    this.pollingActive = false;
    this.destroy$.next();
    console.log('⏹️ Stopped device status polling');
  }

  /**
   * Manually refresh devices
   */
  refresh(): Observable<BiometricDevice[]> {
    return this.loadDevices();
  }

  /**
   * Get device by device ID (not database ID)
   */
  getDeviceByDeviceId(deviceId: string): BiometricDevice | undefined {
    return this.devicesSignal().find(d => d.deviceId === deviceId);
  }

  /**
   * Update device online status (called from SignalR)
   */
  updateDeviceOnlineStatus(deviceId: string, isOnline: boolean): void {
    const devices = this.devicesSignal();
    const device = devices.find(d => d.deviceId === deviceId || d.id === deviceId);

    if (device) {
      device.isOnline = isOnline;
      device.status = isOnline ? DeviceStatus.Online : DeviceStatus.Offline;
      device.lastSyncTime = new Date().toISOString();
      this.devicesSignal.set([...devices]);
      console.log(`🖥️ Device ${deviceId} status: ${isOnline ? 'ONLINE' : 'OFFLINE'}`);
    }
  }

  /**
   * Increment device punch count
   */
  incrementDevicePunchCount(deviceId: string): void {
    const devices = this.devicesSignal();
    const device = devices.find(d => d.deviceId === deviceId || d.id === deviceId);

    if (device) {
      device.punchCountToday++;
      device.lastSyncTime = new Date().toISOString();
      this.devicesSignal.set([...devices]);
    }
  }

  /**
   * Get device health status color
   */
  getDeviceHealthColor(device: BiometricDevice): string {
    if (!device.isOnline || device.status === DeviceStatus.Offline) {
      return 'warn';
    }
    if (device.status === DeviceStatus.Error) {
      return 'error';
    }
    if (device.status === DeviceStatus.Maintenance) {
      return 'accent';
    }
    return 'primary';
  }

  /**
   * Get device status icon
   */
  getDeviceStatusIcon(device: BiometricDevice): string {
    switch (device.status) {
      case DeviceStatus.Online:
        return 'check_circle';
      case DeviceStatus.Offline:
        return 'cancel';
      case DeviceStatus.Error:
        return 'error';
      case DeviceStatus.Maintenance:
        return 'build';
      default:
        return 'help';
    }
  }

  /**
   * Format last sync time
   */
  formatLastSync(lastSyncTime: string): string {
    const now = new Date();
    const syncDate = new Date(lastSyncTime);
    const diffMs = now.getTime() - syncDate.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffMins < 1440) return `${Math.floor(diffMins / 60)}h ago`;
    return `${Math.floor(diffMins / 1440)}d ago`;
  }

  /**
   * Cleanup on service destruction
   */
  ngOnDestroy(): void {
    this.stopPolling();
  }
}
