import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AttendanceMachinesService, AttendanceMachineDto } from '../../../../core/services/attendance-machines.service';
import { BiometricDeviceService, BiometricDeviceDto, DeviceSyncStatusDto } from './biometric-device.service';

@Component({
  selector: 'app-biometric-device-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatChipsModule,
    MatSnackBarModule
  ],
  templateUrl: './biometric-device-list.component.html',
  styleUrls: ['./biometric-device-list.component.scss']
})
export class BiometricDeviceListComponent implements OnInit {
  // Using signals for reactive state management
  machines = signal<AttendanceMachineDto[]>([]);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);
  displayedColumns: string[] = ['machineName', 'machineType', 'location', 'ipAddress', 'status', 'lastSync', 'actions'];

  // Connection testing state
  testingConnection: Set<string> = new Set();
  syncingDevice: Set<string> = new Set();

  constructor(
    private router: Router,
    private snackBar: MatSnackBar,
    private attendanceMachinesService: AttendanceMachinesService,
    private deviceService: BiometricDeviceService
  ) {}

  ngOnInit(): void {
    this.loadDevices();
  }

  /**
   * Load attendance machines using the new AttendanceMachinesService
   */
  private loadDevices(): void {
    this.loading.set(true);
    this.error.set(null);

    this.attendanceMachinesService.getMachines(true).subscribe({
      next: (response) => {
        this.machines.set(response.data);
        this.loading.set(false);
        console.log(`Loaded ${response.total} attendance machines`);
      },
      error: (error) => {
        console.error('Error loading machines:', error);
        this.error.set('Failed to load attendance machines');
        this.loading.set(false);
        this.snackBar.open('Failed to load attendance machines', 'Close', { duration: 3000 });
      }
    });
  }

  /**
   * Navigate to add new device form
   */
  onAdd(): void {
    this.router.navigate(['/tenant/organization/devices/new']);
  }

  /**
   * Navigate to edit device form
   */
  onEdit(id: string): void {
    this.router.navigate(['/tenant/organization/devices', id, 'edit']);
  }

  /**
   * Navigate to device detail view
   */
  onView(id: string): void {
    this.router.navigate(['/tenant/organization/devices', id]);
  }

  /**
   * Delete device with confirmation - Using AttendanceMachinesService
   */
  onDelete(id: string, deviceName: string): void {
    if (confirm(`Are you sure you want to delete the device "${deviceName}"? This action cannot be undone.`)) {
      this.attendanceMachinesService.deleteMachine(id).subscribe({
        next: () => {
          this.snackBar.open('Device deleted successfully', 'Close', { duration: 3000 });
          this.loadDevices();
        },
        error: (error) => {
          console.error('Delete error:', error);
          this.snackBar.open('Failed to delete device', 'Close', { duration: 3000 });
        }
      });
    }
  }

  /**
   * Test device connection
   * Note: This still uses BiometricDeviceService as AttendanceMachinesService doesn't have test connection
   */
  onTestConnection(device: AttendanceMachineDto): void {
    if (!device.ipAddress) {
      this.snackBar.open('Device does not have an IP address configured', 'Close', { duration: 3000 });
      return;
    }

    this.testingConnection.add(device.id);

    const connectionData = {
      ipAddress: device.ipAddress,
      port: device.port || 4370,
      connectionMethod: 'TCP/IP',
      connectionTimeoutSeconds: 30
    };

    this.deviceService.testConnection(connectionData).subscribe({
      next: (result) => {
        this.testingConnection.delete(device.id);
        if (result.success) {
          this.snackBar.open(
            `Connection to ${device.machineName} successful`,
            'Close',
            {
              duration: 3000,
              panelClass: ['success-snackbar']
            }
          );
        } else {
          this.snackBar.open(
            `Failed to connect to ${device.machineName}. ${result.message}`,
            'Close',
            {
              duration: 4000,
              panelClass: ['error-snackbar']
            }
          );
        }
      },
      error: (error) => {
        this.testingConnection.delete(device.id);
        console.error('Connection test error:', error);
        this.snackBar.open(
          `Failed to test connection to ${device.machineName}`,
          'Close',
          {
            duration: 4000,
            panelClass: ['error-snackbar']
          }
        );
      }
    });
  }

  /**
   * Manually trigger device sync
   * Note: This still uses BiometricDeviceService for sync operations
   */
  onSyncNow(device: AttendanceMachineDto): void {
    this.syncingDevice.add(device.id);

    this.deviceService.syncDevice(device.id).subscribe({
      next: (result) => {
        this.syncingDevice.delete(device.id);
        if (result.success) {
          this.snackBar.open(
            `Synced ${result.recordCount} records from ${device.machineName}`,
            'Close',
            {
              duration: 4000,
              panelClass: ['success-snackbar']
            }
          );
          this.loadDevices();
        } else {
          this.snackBar.open(
            `Sync failed for ${device.machineName}. ${result.message}`,
            'Close',
            {
              duration: 4000,
              panelClass: ['error-snackbar']
            }
          );
        }
      },
      error: (error) => {
        this.syncingDevice.delete(device.id);
        console.error('Sync error:', error);
        this.snackBar.open(
          `Failed to sync ${device.machineName}`,
          'Close',
          {
            duration: 4000,
            panelClass: ['error-snackbar']
          }
        );
      }
    });
  }

  /**
   * Check if device is currently being tested
   */
  isTestingConnection(deviceId: string): boolean {
    return this.testingConnection.has(deviceId);
  }

  /**
   * Check if device is currently syncing
   */
  isSyncing(deviceId: string): boolean {
    return this.syncingDevice.has(deviceId);
  }

  /**
   * Determine device status color
   */
  getDeviceStatusClass(device: AttendanceMachineDto): string {
    // Check if device is active
    if (!device.isActive) {
      return 'offline';
    }

    // Check if device has synced recently
    if (!device.lastSyncTime) {
      return 'offline';
    }

    const lastSync = new Date(device.lastSyncTime);
    const now = new Date();
    const minutesSinceSync = (now.getTime() - lastSync.getTime()) / (1000 * 60);

    // Online: synced within last 30 minutes
    if (minutesSinceSync <= 30) {
      return 'online';
    }

    // Warning: synced within last hour
    if (minutesSinceSync <= 60) {
      return 'warning';
    }

    // Offline: exceeded threshold
    return 'offline';
  }

  /**
   * Get human-readable device status text
   */
  getDeviceStatusText(device: AttendanceMachineDto): string {
    const statusClass = this.getDeviceStatusClass(device);

    switch (statusClass) {
      case 'online':
        return 'Online';
      case 'warning':
        return 'Warning';
      case 'offline':
        return 'Offline';
      default:
        return 'Unknown';
    }
  }

  /**
   * Format last sync time as relative time
   */
  getLastSyncDisplay(lastSyncTime?: string): string {
    if (!lastSyncTime) {
      return 'Never';
    }

    const lastSync = new Date(lastSyncTime);
    const now = new Date();
    const diffMs = now.getTime() - lastSync.getTime();
    const diffMinutes = Math.floor(diffMs / (1000 * 60));
    const diffHours = Math.floor(diffMinutes / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMinutes < 1) {
      return 'Just now';
    } else if (diffMinutes < 60) {
      return `${diffMinutes}m ago`;
    } else if (diffHours < 24) {
      return `${diffHours}h ago`;
    } else {
      return `${diffDays}d ago`;
    }
  }

  /**
   * Refresh device list
   */
  onRefresh(): void {
    this.loadDevices();
    this.snackBar.open('Device list refreshed', 'Close', { duration: 2000 });
  }

  /**
   * Get count of online devices
   */
  getOnlineDeviceCount(): number {
    return this.machines().filter(d => this.getDeviceStatusClass(d) === 'online').length;
  }

  /**
   * Get count of warning devices
   */
  getWarningDeviceCount(): number {
    return this.machines().filter(d => this.getDeviceStatusClass(d) === 'warning').length;
  }

  /**
   * Get count of offline devices
   */
  getOfflineDeviceCount(): number {
    return this.machines().filter(d => this.getDeviceStatusClass(d) === 'offline').length;
  }
}
