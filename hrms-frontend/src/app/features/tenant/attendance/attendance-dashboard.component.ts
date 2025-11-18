import { Component, OnInit, OnDestroy, signal, computed, inject, ViewChild, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { Chip, TooltipDirective } from '@app/shared/ui';
import { UiModule } from '../../../shared/ui/ui.module';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ToastService, Divider } from '../../../shared/ui';
import { Subject, takeUntil } from 'rxjs';
import { TableComponent, TableColumn } from '../../../shared/ui/components/table/table';

import { AttendanceRealtimeService } from '../../../core/services/attendance-realtime.service';
import { DeviceStatusService } from '../../../core/services/device-status.service';
import { ThemeService } from '../../../core/services/theme.service';
import { DevicePunchCaptureService, DeviceHealthDto, PunchHistoryDto } from '../../../core/services/device-punch-capture.service';
import { AttendanceMachinesService, AttendanceMachineDto } from '../../../core/services/attendance-machines.service';
import {
  PunchRecord,
  BiometricDevice,
  LiveAttendanceStats,
  PunchType,
  VerificationMethod,
  PunchStatus,
  DeviceStatus
} from '../../../core/models/attendance.model';

interface StatCard {
  title: string;
  icon: string;
  value: number;
  color: string;
  suffix?: string;
  trend?: 'up' | 'down' | 'neutral';
}

/**
 * Production-grade real-time attendance dashboard
 * Features:
 * - Live punch feed via SignalR
 * - Real-time statistics updates
 * - Device status monitoring
 * - Advanced filtering and search
 * - Smooth animations for new data
 * - Dark mode support
 * - Responsive mobile-first design
 */
@Component({
  selector: 'app-attendance-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    TableComponent,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    Chip,
    TooltipDirective,
    MatProgressBarModule,
    UiModule,
    Divider,
  ],
  templateUrl: './attendance-dashboard.component.html',
  styleUrls: ['./attendance-dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AttendanceDashboardComponent implements OnInit, OnDestroy {
  private realtimeService = inject(AttendanceRealtimeService);
  private deviceService = inject(DeviceStatusService);
  private punchCaptureService = inject(DevicePunchCaptureService);
  private attendanceMachinesService = inject(AttendanceMachinesService);
  private themeService = inject(ThemeService);
  private toastService = inject(ToastService);
  private destroy$ = new Subject<void>();

  // Theme
  isDark = this.themeService.isDark;

  // Connection state
  connectionState = this.realtimeService.connectionState;
  isConnected = this.realtimeService.isConnected;
  connectionError = this.realtimeService.error;

  // Data signals
  liveStats = this.realtimeService.liveStats;
  recentPunches = this.realtimeService.recentPunches;
  devices = this.deviceService.devices;
  devicesLoading = this.deviceService.loading;
  lastDeviceUpdate = this.deviceService.lastUpdate;

  // UI state
  loading = signal<boolean>(true);
  error = signal<string | null>(null);
  selectedTab = signal<number>(0);
  searchQuery = signal<string>('');
  selectedDevice = signal<string>('all');
  selectedVerificationMethod = signal<string>('all');
  selectedPunchType = signal<string>('all');

  // Device health and status
  deviceHealth = signal<DeviceHealthDto | null>(null);
  healthCheckLoading = signal<boolean>(false);
  attendanceMachines = signal<AttendanceMachineDto[]>([]);
  machinesLoading = signal<boolean>(false);
  punchHistory = signal<PunchHistoryDto[]>([]);
  punchHistoryLoading = signal<boolean>(false);

  // Table columns
  tableColumns: TableColumn[] = [
    { key: 'timestamp', label: 'Time', sortable: true },
    { key: 'employeeName', label: 'Employee', sortable: true },
    { key: 'deviceName', label: 'Device', sortable: true },
    { key: 'punchType', label: 'Type', sortable: true },
    { key: 'verificationMethod', label: 'Method', sortable: true },
    { key: 'verificationQuality', label: 'Quality', sortable: true },
    { key: 'status', label: 'Status', sortable: true }
  ];

  // Filtered punches for table
  filteredPunches = signal<PunchRecord[]>([]);

  // Enums for template
  PunchType = PunchType;
  VerificationMethod = VerificationMethod;
  PunchStatus = PunchStatus;
  DeviceStatus = DeviceStatus;

  // Filter options
  verificationMethods = Object.values(VerificationMethod);
  punchTypes = Object.values(PunchType);

  // Computed stats cards
  statCards = computed<StatCard[]>(() => {
    const stats = this.liveStats();
    if (!stats) {
      return this.getEmptyStatCards();
    }

    return [
      {
        title: 'Present Today',
        icon: 'check_circle',
        value: stats.presentToday,
        color: 'success',
        trend: 'neutral'
      },
      {
        title: 'Absent Today',
        icon: 'cancel',
        value: stats.absentToday,
        color: 'warn',
        trend: 'neutral'
      },
      {
        title: 'Late Arrivals',
        icon: 'schedule',
        value: stats.lateArrivals,
        color: 'warning',
        trend: 'neutral'
      },
      {
        title: 'On Leave',
        icon: 'beach_access',
        value: stats.onLeave,
        color: 'info',
        trend: 'neutral'
      },
      {
        title: 'Total Punches',
        icon: 'touch_app',
        value: stats.totalPunchesToday,
        color: 'primary',
        trend: 'up'
      },
      {
        title: 'Avg Verification',
        icon: 'verified',
        value: Math.round(stats.averageVerificationQuality),
        color: 'accent',
        suffix: '%',
        trend: stats.averageVerificationQuality >= 90 ? 'up' : 'neutral'
      }
    ];
  });

  // Computed device stats
  deviceStats = computed(() => {
    const onlineDevices = this.deviceService.onlineDevices();
    const offlineDevices = this.deviceService.offlineDevices();
    const totalDevices = this.deviceService.totalDevices();
    const healthPercentage = this.deviceService.healthPercentage();

    return {
      online: onlineDevices.length,
      offline: offlineDevices.length,
      total: totalDevices,
      healthPercentage
    };
  });

  // Filtered devices for dropdown
  deviceOptions = computed(() => {
    return this.devices().filter(d => d.isOnline);
  });

  ngOnInit(): void {
    console.log('🚀 Attendance Dashboard initializing...');
    this.initializeDashboard();
  }

  ngOnDestroy(): void {
    console.log('🛑 Attendance Dashboard destroying...');
    this.destroy$.next();
    this.destroy$.complete();
    this.realtimeService.disconnect();
    this.deviceService.stopPolling();
  }

  /**
   * Initialize dashboard - connect to SignalR and load data
   */
  private async initializeDashboard(): Promise<void> {
    try {
      this.loading.set(true);
      this.error.set(null);

      // Start device polling
      this.deviceService.startPolling();

      // Load attendance machines
      await this.loadAttendanceMachines();

      // Check device health
      await this.checkDeviceHealth();

      // Connect to SignalR hub
      await this.realtimeService.connect();

      // Subscribe to real-time events
      this.subscribeToRealtimeEvents();

      // Setup table data source
      this.setupTableDataSource();

      this.loading.set(false);
      console.log('✅ Dashboard initialized successfully');

      this.showNotification('Connected to real-time attendance feed', 'success');
    } catch (error) {
      console.error('❌ Failed to initialize dashboard:', error);
      this.error.set(error instanceof Error ? error.message : 'Initialization failed');
      this.loading.set(false);
      this.showNotification('Failed to connect to attendance system', 'error');
    }
  }

  /**
   * Load attendance machines
   */
  private async loadAttendanceMachines(): Promise<void> {
    try {
      this.machinesLoading.set(true);
      const response = await this.attendanceMachinesService.getMachines(true).toPromise();
      if (response) {
        this.attendanceMachines.set(response.data);
        console.log(`✅ Loaded ${response.total} attendance machines`);
      }
    } catch (error) {
      console.error('❌ Failed to load attendance machines:', error);
    } finally {
      this.machinesLoading.set(false);
    }
  }

  /**
   * Check device health status
   */
  private async checkDeviceHealth(): Promise<void> {
    try {
      this.healthCheckLoading.set(true);
      const health = await this.punchCaptureService.getHealth().toPromise();
      if (health) {
        this.deviceHealth.set(health);
        console.log('✅ Device health check:', health.status);
      }
    } catch (error) {
      console.error('❌ Device health check failed:', error);
    } finally {
      this.healthCheckLoading.set(false);
    }
  }

  /**
   * Subscribe to real-time events
   */
  private subscribeToRealtimeEvents(): void {
    // New punch received
    this.realtimeService.newPunch$
      .pipe(takeUntil(this.destroy$))
      .subscribe(punch => {
        console.log('📥 New punch:', punch);
        this.handleNewPunch(punch);
      });

    // Stats updated
    this.realtimeService.statsUpdate$
      .pipe(takeUntil(this.destroy$))
      .subscribe(stats => {
        console.log('📊 Stats updated:', stats);
      });

    // Connection status changed
    this.realtimeService.connectionStatus$
      .pipe(takeUntil(this.destroy$))
      .subscribe(connected => {
        if (connected) {
          this.showNotification('Reconnected to attendance feed', 'success');
        } else {
          this.showNotification('Disconnected from attendance feed', 'warning');
        }
      });
  }

  /**
   * Handle new punch received
   */
  private handleNewPunch(punch: PunchRecord): void {
    // Update table data
    this.updateTableData();

    // Update device punch count
    this.deviceService.incrementDevicePunchCount(punch.deviceId);

    // Show notification for important punches
    if (punch.status === PunchStatus.Late) {
      this.showNotification(
        `Late arrival: ${punch.employeeName}`,
        'warning'
      );
    }

    // Play sound notification (optional)
    this.playNotificationSound();
  }

  /**
   * Setup table data source with sorting and pagination
   */
  private setupTableDataSource(): void {
    // Update table when punches change
    this.recentPunches().forEach(() => {
      this.updateTableData();
    });
  }

  /**
   * Update table data source
   */
  private updateTableData(): void {
    let punches = [...this.recentPunches()];

    // Apply filters
    const searchQuery = this.searchQuery().toLowerCase();
    const deviceFilter = this.selectedDevice();
    const methodFilter = this.selectedVerificationMethod();
    const typeFilter = this.selectedPunchType();

    if (searchQuery) {
      punches = punches.filter(p =>
        p.employeeName.toLowerCase().includes(searchQuery) ||
        p.employeeCode?.toLowerCase().includes(searchQuery) ||
        p.deviceName.toLowerCase().includes(searchQuery)
      );
    }

    if (deviceFilter !== 'all') {
      punches = punches.filter(p => p.deviceId === deviceFilter);
    }

    if (methodFilter !== 'all') {
      punches = punches.filter(p => p.verificationMethod === methodFilter);
    }

    if (typeFilter !== 'all') {
      punches = punches.filter(p => p.punchType === typeFilter);
    }

    this.filteredPunches.set(punches);
  }

  /**
   * Apply filters
   */
  applyFilters(): void {
    this.updateTableData();
  }

  /**
   * Clear all filters
   */
  clearFilters(): void {
    this.searchQuery.set('');
    this.selectedDevice.set('all');
    this.selectedVerificationMethod.set('all');
    this.selectedPunchType.set('all');
    this.updateTableData();
  }

  /**
   * Refresh all data
   */
  async refresh(): Promise<void> {
    try {
      await Promise.all([
        this.realtimeService.refreshStats(),
        this.realtimeService.refreshPunches(),
        this.deviceService.refresh().toPromise(),
        this.loadAttendanceMachines(),
        this.checkDeviceHealth()
      ]);
      this.showNotification('Data refreshed successfully', 'success');
    } catch (error) {
      console.error('❌ Failed to refresh data:', error);
      this.showNotification('Failed to refresh data', 'error');
    }
  }

  /**
   * Load punch history for a specific machine
   */
  async loadPunchHistory(machineId: string, apiKey: string, hours: number = 24): Promise<void> {
    try {
      this.punchHistoryLoading.set(true);
      const response = await this.punchCaptureService.getPunchHistory(apiKey, hours, 1, 50).toPromise();
      if (response) {
        this.punchHistory.set(response.data);
        console.log(`✅ Loaded ${response.data.length} punch records`);
      }
    } catch (error) {
      console.error('❌ Failed to load punch history:', error);
      this.showNotification('Failed to load punch history', 'error');
    } finally {
      this.punchHistoryLoading.set(false);
    }
  }

  /**
   * Get machine status (online/offline)
   */
  getMachineStatus(machine: AttendanceMachineDto): 'online' | 'offline' | 'warning' {
    if (!machine.lastSyncAt) {
      return 'offline';
    }

    const lastSync = new Date(machine.lastSyncAt);
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

    // Offline: no sync in over an hour
    return 'offline';
  }

  /**
   * Get formatted last sync time
   */
  getLastSyncDisplay(lastSyncTime?: string): string {
    if (!lastSyncTime) {
      return 'Never';
    }

    const lastSync = new Date(lastSyncTime);
    const now = new Date();
    const diffMs = now.getTime() - lastSync.getTime();
    const diffMinutes = Math.floor(diffMs / (1000 * 60));

    if (diffMinutes < 1) {
      return 'Just now';
    } else if (diffMinutes < 60) {
      return `${diffMinutes}m ago`;
    } else {
      const diffHours = Math.floor(diffMinutes / 60);
      return `${diffHours}h ago`;
    }
  }

  /**
   * Reconnect to SignalR
   */
  async reconnect(): Promise<void> {
    try {
      await this.realtimeService.disconnect();
      await this.realtimeService.connect();
      this.showNotification('Reconnected successfully', 'success');
    } catch (error) {
      console.error('❌ Failed to reconnect:', error);
      this.showNotification('Failed to reconnect', 'error');
    }
  }

  /**
   * Test device connection
   */
  async testDevice(device: BiometricDevice): Promise<void> {
    try {
      await this.deviceService.testDeviceConnection(device.id).toPromise();
      this.showNotification(`Device ${device.name} tested successfully`, 'success');
    } catch (error) {
      console.error(`❌ Device test failed:`, error);
      this.showNotification(`Device ${device.name} test failed`, 'error');
    }
  }

  /**
   * Sync device data
   */
  async syncDevice(device: BiometricDevice): Promise<void> {
    try {
      await this.deviceService.syncDevice(device.id).toPromise();
      this.showNotification(`Device ${device.name} synced successfully`, 'success');
    } catch (error) {
      console.error(`❌ Device sync failed:`, error);
      this.showNotification(`Device ${device.name} sync failed`, 'error');
    }
  }

  /**
   * Get empty stat cards
   */
  private getEmptyStatCards(): StatCard[] {
    return [
      { title: 'Present Today', icon: 'check_circle', value: 0, color: 'success' },
      { title: 'Absent Today', icon: 'cancel', value: 0, color: 'warn' },
      { title: 'Late Arrivals', icon: 'schedule', value: 0, color: 'warning' },
      { title: 'On Leave', icon: 'beach_access', value: 0, color: 'info' },
      { title: 'Total Punches', icon: 'touch_app', value: 0, color: 'primary' },
      { title: 'Avg Verification', icon: 'verified', value: 0, color: 'accent', suffix: '%' }
    ];
  }

  /**
   * Get punch type color
   */
  getPunchTypeColor(punchType: PunchType): 'primary' | 'success' | 'warning' | 'error' | 'neutral' {
    switch (punchType) {
      case PunchType.CheckIn:
        return 'success';
      case PunchType.CheckOut:
        return 'error';
      case PunchType.Break:
        return 'warning';
      case PunchType.BreakReturn:
        return 'primary';
      default:
        return 'neutral';
    }
  }

  /**
   * Get punch status color
   */
  getPunchStatusColor(status: PunchStatus): 'primary' | 'success' | 'warning' | 'error' | 'neutral' {
    switch (status) {
      case PunchStatus.Valid:
        return 'success';
      case PunchStatus.Late:
        return 'warning';
      case PunchStatus.Early:
        return 'primary';
      case PunchStatus.Invalid:
        return 'error';
      case PunchStatus.Duplicate:
        return 'error';
      default:
        return 'neutral';
    }
  }

  /**
   * Get verification method icon
   */
  getVerificationMethodIcon(method: VerificationMethod): string {
    switch (method) {
      case VerificationMethod.Fingerprint:
        return 'fingerprint';
      case VerificationMethod.Face:
        return 'face';
      case VerificationMethod.Card:
        return 'credit_card';
      case VerificationMethod.PIN:
        return 'pin';
      case VerificationMethod.Mobile:
        return 'smartphone';
      case VerificationMethod.Web:
        return 'computer';
      default:
        return 'help';
    }
  }

  /**
   * Get device status color
   */
  getDeviceStatusColor(device: BiometricDevice): string {
    return this.deviceService.getDeviceHealthColor(device);
  }

  /**
   * Get device status icon
   */
  getDeviceStatusIcon(device: BiometricDevice): string {
    return this.deviceService.getDeviceStatusIcon(device);
  }

  /**
   * Format timestamp
   */
  formatTime(timestamp: string): string {
    const date = new Date(timestamp);
    return date.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  }

  /**
   * Format date
   */
  formatDate(timestamp: string): string {
    const date = new Date(timestamp);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric'
    });
  }

  /**
   * Format last sync time
   */
  formatLastSync(lastSyncTime: string): string {
    return this.deviceService.formatLastSync(lastSyncTime);
  }

  /**
   * Get quality color based on percentage
   */
  getQualityColor(quality: number): string {
    if (quality >= 90) return 'success';
    if (quality >= 70) return 'accent';
    if (quality >= 50) return 'warning';
    return 'warn';
  }

  /**
   * Show notification
   */
  private showNotification(message: string, type: 'success' | 'error' | 'warning' | 'info'): void {
    switch (type) {
      case 'success':
        this.toastService.success(message, 3000);
        break;
      case 'error':
        this.toastService.error(message, 3000);
        break;
      case 'warning':
        this.toastService.warning(message, 3000);
        break;
      case 'info':
        this.toastService.info(message, 3000);
        break;
    }
  }

  /**
   * Play notification sound (optional)
   */
  private playNotificationSound(): void {
    // Implement sound notification if needed
    // const audio = new Audio('assets/sounds/notification.mp3');
    // audio.play().catch(err => console.log('Sound play failed:', err));
  }

  /**
   * Export punches to CSV
   */
  exportToCSV(): void {
    const punches = this.filteredPunches();
    if (punches.length === 0) {
      this.showNotification('No data to export', 'warning');
      return;
    }

    const headers = ['Timestamp', 'Employee', 'Device', 'Type', 'Method', 'Quality', 'Status'];
    const rows = punches.map((p: PunchRecord) => [
      p.timestamp,
      p.employeeName,
      p.deviceName,
      p.punchType,
      p.verificationMethod,
      p.verificationQuality,
      p.status
    ]);

    const csv = [
      headers.join(','),
      ...rows.map((row: any[]) => row.join(','))
    ].join('\n');

    const blob = new Blob([csv], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `attendance-${new Date().toISOString()}.csv`;
    a.click();
    window.URL.revokeObjectURL(url);

    this.showNotification('Data exported successfully', 'success');
  }
}
