import { Injectable, inject, signal, computed, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, BehaviorSubject, Observable } from 'rxjs';
import { PunchRecord, LiveAttendanceStats } from '../models/attendance.model';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

/**
 * Real-time attendance service using SignalR
 * Handles WebSocket connection to attendance hub
 * Provides live updates for punch records and statistics
 */
@Injectable({
  providedIn: 'root'
})
export class AttendanceRealtimeService implements OnDestroy {
  private authService = inject(AuthService);

  // SignalR connection
  private connection: signalR.HubConnection | null = null;
  private reconnectAttempts = 0;
  private readonly MAX_RECONNECT_ATTEMPTS = 5;

  // Connection state signals
  private connectionStateSignal = signal<'disconnected' | 'connecting' | 'connected' | 'reconnecting'>('disconnected');
  private errorSignal = signal<string | null>(null);

  // Data signals
  private recentPunchesSignal = signal<PunchRecord[]>([]);
  private liveStatsSignal = signal<LiveAttendanceStats | null>(null);

  // Subjects for real-time events
  private newPunchSubject = new Subject<PunchRecord>();
  private statsUpdateSubject = new Subject<LiveAttendanceStats>();
  private connectionStatusSubject = new BehaviorSubject<boolean>(false);

  // Public readonly signals
  readonly connectionState = this.connectionStateSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly recentPunches = this.recentPunchesSignal.asReadonly();
  readonly liveStats = this.liveStatsSignal.asReadonly();
  readonly isConnected = computed(() => this.connectionStateSignal() === 'connected');

  // Public observables
  readonly newPunch$ = this.newPunchSubject.asObservable();
  readonly statsUpdate$ = this.statsUpdateSubject.asObservable();
  readonly connectionStatus$ = this.connectionStatusSubject.asObservable();

  constructor() {
    console.log('🔵 AttendanceRealtimeService initialized');
  }

  /**
   * Start SignalR connection to attendance hub
   */
  async connect(): Promise<void> {
    if (this.connection) {
      console.log('⚠️ Connection already exists');
      return;
    }

    try {
      this.connectionStateSignal.set('connecting');
      this.errorSignal.set(null);

      const token = this.authService.getToken();
      if (!token) {
        throw new Error('No authentication token available');
      }

      // Build SignalR connection with automatic reconnection
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${environment.apiUrl}/hubs/attendance`, {
          accessTokenFactory: () => token,
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents,
          skipNegotiation: false
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            if (retryContext.previousRetryCount >= this.MAX_RECONNECT_ATTEMPTS) {
              console.error('❌ Maximum reconnection attempts reached');
              return null; // Stop reconnecting
            }
            // Exponential backoff: 2s, 4s, 8s, 16s, 32s
            return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 32000);
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Register event handlers
      this.registerEventHandlers();

      // Start connection
      await this.connection.start();

      this.connectionStateSignal.set('connected');
      this.connectionStatusSubject.next(true);
      this.reconnectAttempts = 0;

      console.log('✅ SignalR connected to attendance hub');

      // Request initial data
      await this.requestInitialData();

    } catch (error) {
      console.error('❌ Failed to connect to attendance hub:', error);
      this.connectionStateSignal.set('disconnected');
      this.errorSignal.set(error instanceof Error ? error.message : 'Connection failed');
      this.connectionStatusSubject.next(false);
      throw error;
    }
  }

  /**
   * Disconnect from SignalR hub
   */
  async disconnect(): Promise<void> {
    if (!this.connection) {
      return;
    }

    try {
      await this.connection.stop();
      this.connection = null;
      this.connectionStateSignal.set('disconnected');
      this.connectionStatusSubject.next(false);
      console.log('✅ SignalR disconnected from attendance hub');
    } catch (error) {
      console.error('❌ Error disconnecting from hub:', error);
    }
  }

  /**
   * Register SignalR event handlers
   */
  private registerEventHandlers(): void {
    if (!this.connection) return;

    // Handle new punch events
    this.connection.on('NewPunch', (punch: PunchRecord) => {
      console.log('📥 New punch received:', punch);

      // Mark as new for animation
      punch.isNew = true;

      // Add to recent punches (keep last 50)
      const currentPunches = this.recentPunchesSignal();
      const updatedPunches = [punch, ...currentPunches].slice(0, 50);
      this.recentPunchesSignal.set(updatedPunches);

      // Emit event
      this.newPunchSubject.next(punch);

      // Remove new flag after animation
      setTimeout(() => {
        punch.isNew = false;
        this.recentPunchesSignal.set([...this.recentPunchesSignal()]);
      }, 3000);
    });

    // Handle statistics updates
    this.connection.on('StatsUpdate', (stats: LiveAttendanceStats) => {
      console.log('📊 Stats updated:', stats);
      this.liveStatsSignal.set(stats);
      this.statsUpdateSubject.next(stats);
    });

    // Handle device status updates
    this.connection.on('DeviceStatusChanged', (data: { deviceId: string; isOnline: boolean }) => {
      console.log('🖥️ Device status changed:', data);
      // This will be handled by device-status.service
    });

    // Connection lifecycle events
    this.connection.onreconnecting((error) => {
      console.warn('⚠️ SignalR reconnecting...', error);
      this.connectionStateSignal.set('reconnecting');
      this.connectionStatusSubject.next(false);
      this.reconnectAttempts++;
    });

    this.connection.onreconnected((connectionId) => {
      console.log('✅ SignalR reconnected:', connectionId);
      this.connectionStateSignal.set('connected');
      this.connectionStatusSubject.next(true);
      this.reconnectAttempts = 0;
      this.errorSignal.set(null);

      // Refresh data after reconnection
      this.requestInitialData().catch(err => {
        console.error('Failed to refresh data after reconnection:', err);
      });
    });

    this.connection.onclose((error) => {
      console.error('❌ SignalR connection closed:', error);
      this.connectionStateSignal.set('disconnected');
      this.connectionStatusSubject.next(false);

      if (error) {
        this.errorSignal.set(error.message || 'Connection closed');
      }
    });
  }

  /**
   * Request initial data from server
   */
  private async requestInitialData(): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      // Request recent punches
      const recentPunches = await this.connection.invoke<PunchRecord[]>('GetRecentPunches', 50);
      this.recentPunchesSignal.set(recentPunches || []);

      // Request live statistics
      const liveStats = await this.connection.invoke<LiveAttendanceStats>('GetLiveStats');
      this.liveStatsSignal.set(liveStats);

      console.log('✅ Initial data loaded:', {
        punches: recentPunches?.length || 0,
        stats: liveStats
      });
    } catch (error) {
      console.error('❌ Failed to load initial data:', error);
      this.errorSignal.set('Failed to load initial data');
    }
  }

  /**
   * Manually refresh statistics
   */
  async refreshStats(): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Not connected to hub');
    }

    try {
      const stats = await this.connection.invoke<LiveAttendanceStats>('GetLiveStats');
      this.liveStatsSignal.set(stats);
      return Promise.resolve();
    } catch (error) {
      console.error('❌ Failed to refresh stats:', error);
      throw error;
    }
  }

  /**
   * Manually refresh recent punches
   */
  async refreshPunches(): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Not connected to hub');
    }

    try {
      const punches = await this.connection.invoke<PunchRecord[]>('GetRecentPunches', 50);
      this.recentPunchesSignal.set(punches || []);
      return Promise.resolve();
    } catch (error) {
      console.error('❌ Failed to refresh punches:', error);
      throw error;
    }
  }

  /**
   * Subscribe to specific employee's punches
   */
  async subscribeToEmployee(employeeId: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Not connected to hub');
    }

    try {
      await this.connection.invoke('SubscribeToEmployee', employeeId);
      console.log(`✅ Subscribed to employee: ${employeeId}`);
    } catch (error) {
      console.error('❌ Failed to subscribe to employee:', error);
      throw error;
    }
  }

  /**
   * Unsubscribe from employee's punches
   */
  async unsubscribeFromEmployee(employeeId: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.invoke('UnsubscribeFromEmployee', employeeId);
      console.log(`✅ Unsubscribed from employee: ${employeeId}`);
    } catch (error) {
      console.error('❌ Failed to unsubscribe from employee:', error);
    }
  }

  /**
   * Subscribe to specific device's punches
   */
  async subscribeToDevice(deviceId: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Not connected to hub');
    }

    try {
      await this.connection.invoke('SubscribeToDevice', deviceId);
      console.log(`✅ Subscribed to device: ${deviceId}`);
    } catch (error) {
      console.error('❌ Failed to subscribe to device:', error);
      throw error;
    }
  }

  /**
   * Clear all recent punches
   */
  clearPunches(): void {
    this.recentPunchesSignal.set([]);
  }

  /**
   * Get connection state as string
   */
  getConnectionState(): string {
    return this.connection?.state || 'Disconnected';
  }

  ngOnDestroy(): void {
    this.disconnect();
    this.newPunchSubject.complete();
    this.statsUpdateSubject.complete();
    this.connectionStatusSubject.complete();
  }
}
