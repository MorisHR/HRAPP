import { Component, OnInit, AfterViewInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatRadioModule } from '@angular/material/radio';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { LocationService, LocationDropdownDto } from '../locations/location.service';
import { AttendanceMachinesService, CreateAttendanceMachineDto, UpdateAttendanceMachineDto } from '../../../../core/services/attendance-machines.service';
import { BiometricDeviceService, CreateBiometricDeviceDto, UpdateBiometricDeviceDto, TestConnectionDto } from './biometric-device.service';
import { DeviceApiKeysComponent } from './device-api-keys.component';

// Device Types
export const DEVICE_TYPES = [
  { value: 'ZKTeco', label: 'ZKTeco' },
  { value: 'Fingerprint', label: 'Fingerprint Scanner' },
  { value: 'Face Recognition', label: 'Face Recognition' },
  { value: 'Card Reader', label: 'Card Reader' },
  { value: 'Hybrid', label: 'Hybrid (Multi-Modal)' }
];

// Connection Methods
export const CONNECTION_METHODS = [
  { value: 'TCP/IP', label: 'TCP/IP (Network)' },
  { value: 'USB', label: 'USB' },
  { value: 'Serial', label: 'Serial Port' },
  { value: 'WiFi', label: 'WiFi' }
];

@Component({
  selector: 'app-biometric-device-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatRadioModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatSnackBarModule,
    MatTooltipModule,
    MatSlideToggleModule,
    DeviceApiKeysComponent
],
  templateUrl: './biometric-device-form.component.html',
  styleUrls: ['./biometric-device-form.component.scss']
})
export class BiometricDeviceFormComponent implements OnInit, AfterViewInit {
  deviceForm!: FormGroup;
  isEditMode = false;
  deviceId?: string;
  loading = false;
  testingConnection = false;
  savingDevice = false;
  hidePassword = true;
  locations: LocationDropdownDto[] = [];
  loadingLocations = false;

  // Dropdown options
  deviceTypes = DEVICE_TYPES;
  connectionMethods = CONNECTION_METHODS;

  // Form validation patterns
  private ipAddressPattern = /^(\d{1,3}\.){3}\d{1,3}$/;
  private macAddressPattern = /^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$/;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private locationService: LocationService,
    private attendanceMachinesService: AttendanceMachinesService,
    private deviceService: BiometricDeviceService,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadLocations();
    this.checkEditMode();
    this.setupFormListeners();
  }

  /**
   * Fix change detection error by triggering detection after view init
   */
  ngAfterViewInit(): void {
    // Trigger change detection to fix ExpressionChangedAfterItHasBeenCheckedError
    this.cdr.detectChanges();
  }

  /**
   * Initialize reactive form with validators
   * Comprehensive form matching the HTML template
   */
  private initForm(): void {
    this.deviceForm = this.fb.group({
      // Basic Device Information
      deviceCode: ['', [Validators.required, Validators.maxLength(50)]],
      machineName: ['', [Validators.required, Validators.maxLength(100)]],
      deviceType: ['ZKTeco', Validators.required], // Used in template
      model: [''],
      departmentId: [''],
      locationId: ['', Validators.required],

      // Network Configuration
      connectionMethod: ['TCP/IP', Validators.required],
      ipAddress: ['', [Validators.pattern(this.ipAddressPattern), this.ipAddressValidator.bind(this)]],
      port: [4370, [Validators.min(1), Validators.max(65535)]],
      macAddress: ['', Validators.pattern(this.macAddressPattern)],

      // Device Credentials (Optional)
      username: [''],
      password: [''],

      // Sync Settings
      syncEnabled: [true],
      syncIntervalMinutes: [15, [Validators.required, Validators.min(1), Validators.max(1440)]],
      connectionTimeoutSeconds: [30, [Validators.required, Validators.min(5), Validators.max(300)]],
      offlineAlertEnabled: [true],
      offlineThresholdMinutes: [60, [Validators.required, Validators.min(5), Validators.max(1440)]],

      // Device Identification (Optional)
      zkTecoDeviceId: ['', Validators.maxLength(50)],
      serialNumber: ['', Validators.maxLength(100)],
      firmwareVersion: [''],

      // Status
      isActive: [true]
    });
  }

  /**
   * Custom IP address validator
   */
  private ipAddressValidator(control: any) {
    if (!control.value) {
      return null;
    }

    const parts = control.value.split('.');
    if (parts.length !== 4) {
      return { invalidIp: true };
    }

    const valid = parts.every((part: string) => {
      const num = parseInt(part, 10);
      return num >= 0 && num <= 255;
    });

    return valid ? null : { invalidIp: true };
  }

  /**
   * Setup form value change listeners
   */
  private setupFormListeners(): void {
    // Simplified - no dynamic validators needed for AttendanceMachinesService
    // All fields are optional except machineName and machineType
  }

  /**
   * Load location dropdown options
   */
  private loadLocations(): void {
    this.loadingLocations = true;
    this.locationService.getDropdown().subscribe({
      next: (locations) => {
        this.locations = locations;
        this.loadingLocations = false;
      },
      error: (error) => {
        console.error('Error loading locations:', error);
        this.snackBar.open('Failed to load locations', 'Close', { duration: 3000 });
        this.loadingLocations = false;
      }
    });
  }

  /**
   * Check if in edit mode and load device data
   */
  private checkEditMode(): void {
    this.deviceId = this.route.snapshot.paramMap.get('id') || undefined;

    if (this.deviceId) {
      this.isEditMode = true;
      this.loadDeviceData(this.deviceId);
    }
  }

  /**
   * Load existing device data for editing - Using AttendanceMachinesService
   */
  private loadDeviceData(id: string): void {
    this.loading = true;

    this.attendanceMachinesService.getMachineById(id).subscribe({
      next: (machine) => {
        this.deviceForm.patchValue({
          machineName: machine.machineName,
          departmentId: machine.departmentId,
          ipAddress: machine.ipAddress,
          port: machine.port,
          zkTecoDeviceId: machine.zkTecoDeviceId,
          serialNumber: machine.serialNumber,
          isActive: machine.isActive
        });
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading machine:', error);
        this.snackBar.open('Failed to load machine data', 'Close', { duration: 3000 });
        this.loading = false;
      }
    });
  }

  /**
   * Test device connection
   * Note: Uses BiometricDeviceService for connection testing
   */
  onTestConnection(): void {
    // Validate required network fields
    const ipAddress = this.deviceForm.get('ipAddress')?.value;
    const port = this.deviceForm.get('port')?.value;

    if (!ipAddress || !port) {
      this.snackBar.open('Please enter IP Address and Port to test connection', 'Close', {
        duration: 3000
      });
      return;
    }

    // Validate IP format
    if (this.deviceForm.get('ipAddress')?.invalid) {
      this.snackBar.open('Please enter a valid IP Address', 'Close', {
        duration: 3000
      });
      return;
    }

    this.testingConnection = true;

    const connectionData: TestConnectionDto = {
      ipAddress,
      port,
      connectionMethod: 'TCP/IP',
      connectionTimeoutSeconds: 30
    };

    this.deviceService.testConnection(connectionData).subscribe({
      next: (result) => {
        // Wrap in setTimeout to avoid change detection error
        setTimeout(() => {
          this.testingConnection = false;
          this.cdr.detectChanges();
        });

        if (result.success) {
          this.snackBar.open(
            `Successfully connected to device at ${ipAddress}:${port}`,
            'Close',
            {
              duration: 5000,
              panelClass: ['success-snackbar']
            }
          );
        } else {
          this.snackBar.open(
            `Failed to connect to device at ${ipAddress}:${port}. ${result.message}`,
            'Close',
            {
              duration: 6000,
              panelClass: ['error-snackbar']
            }
          );
        }
      },
      error: (error) => {
        // Wrap in setTimeout to avoid change detection error
        setTimeout(() => {
          this.testingConnection = false;
          this.cdr.detectChanges();
        });

        console.error('Connection test error:', error);
        this.snackBar.open(
          `Failed to connect to device at ${ipAddress}:${port}. Please check IP, port, and network settings.`,
          'Close',
          {
            duration: 6000,
            panelClass: ['error-snackbar']
          }
        );
      }
    });
  }

  /**
   * Submit form to create or update device - Using AttendanceMachinesService
   */
  onSubmit(): void {
    if (this.deviceForm.invalid) {
      // Mark all fields as touched to show validation errors
      Object.keys(this.deviceForm.controls).forEach(key => {
        this.deviceForm.get(key)?.markAsTouched();
      });

      this.snackBar.open('Please fix all validation errors before submitting', 'Close', {
        duration: 4000
      });
      return;
    }

    this.savingDevice = true;
    const formData = this.deviceForm.value;

    // Prepare machine DTO for AttendanceMachinesService
    // Backend expects: MachineName (required), MachineId (required for create), Location (string), etc.
    const machineDto: any = {
      machineName: formData.machineName!,
      ipAddress: formData.ipAddress || undefined,
      location: undefined, // TODO: Map locationId to location string if available
      departmentId: formData.departmentId || undefined,
      model: formData.model || undefined,
      port: formData.port || undefined,
      isActive: formData.isActive ?? true
    };

    // For CREATE: machineId is required
    if (!this.isEditMode) {
      machineDto.machineId = formData.deviceCode || formData.zkTecoDeviceId || `DEVICE-${Date.now()}`;
      machineDto.zkTecoDeviceId = formData.zkTecoDeviceId || undefined;
      machineDto.serialNumber = formData.serialNumber || undefined;
    }

    console.log('Submitting machine DTO:', machineDto);
    console.log('Submitting machine DTO (JSON):', JSON.stringify(machineDto, null, 2));

    const saveOperation = this.isEditMode
      ? this.attendanceMachinesService.updateMachine(this.deviceId!, machineDto as UpdateAttendanceMachineDto)
      : this.attendanceMachinesService.createMachine(machineDto as CreateAttendanceMachineDto);

    saveOperation.subscribe({
      next: (response) => {
        // Wrap in setTimeout to avoid change detection error
        setTimeout(() => {
          this.savingDevice = false;
          this.cdr.detectChanges();
        });

        const action = this.isEditMode ? 'updated' : 'created';
        this.snackBar.open(
          `Attendance machine ${action} successfully`,
          'Close',
          {
            duration: 3000,
            panelClass: ['success-snackbar']
          }
        );
        this.router.navigate(['/tenant/organization/devices']);
      },
      error: (error) => {
        // Wrap in setTimeout to avoid change detection error
        setTimeout(() => {
          this.savingDevice = false;
          this.cdr.detectChanges();
        });

        console.error('Save error:', error);
        console.error('Error details:', error?.error);
        console.error('Validation errors:', JSON.stringify(error?.error?.errors, null, 2));
        const errorMessage = error?.error?.message || error?.error?.title || `Failed to ${this.isEditMode ? 'update' : 'create'} machine`;
        this.snackBar.open(
          errorMessage,
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
   * Cancel form and navigate back
   */
  onCancel(): void {
    if (this.deviceForm.dirty) {
      const confirmLeave = confirm('You have unsaved changes. Are you sure you want to leave?');
      if (!confirmLeave) {
        return;
      }
    }

    this.router.navigate(['/tenant/organization/devices']);
  }

  /**
   * Get form control for template access
   */
  getControl(name: string) {
    return this.deviceForm.get(name);
  }

  /**
   * Check if field has error
   */
  hasError(fieldName: string, errorType: string): boolean {
    const control = this.deviceForm.get(fieldName);
    return !!(control?.hasError(errorType) && control?.touched);
  }

  /**
   * Get error message for field
   */
  getErrorMessage(fieldName: string): string {
    const control = this.deviceForm.get(fieldName);

    if (!control || !control.errors || !control.touched) {
      return '';
    }

    if (control.hasError('required')) {
      return 'This field is required';
    }

    if (control.hasError('pattern')) {
      if (fieldName === 'ipAddress') {
        return 'Please enter a valid IP address (e.g., 192.168.1.100)';
      }
      if (fieldName === 'macAddress') {
        return 'Please enter a valid MAC address (e.g., 00:17:61:01:23:45)';
      }
      return 'Invalid format';
    }

    if (control.hasError('invalidIp')) {
      return 'Invalid IP address. Each segment must be 0-255';
    }

    if (control.hasError('min')) {
      return `Minimum value is ${control.errors['min'].min}`;
    }

    if (control.hasError('max')) {
      return `Maximum value is ${control.errors['max'].max}`;
    }

    if (control.hasError('maxlength')) {
      return `Maximum length is ${control.errors['maxlength'].requiredLength} characters`;
    }

    return 'Invalid value';
  }

  /**
   * Check if network fields should be enabled (always true for TCP/IP devices)
   */
  isNetworkConfigRequired(): boolean {
    return true; // Always show network config for attendance machines
  }

  /**
   * Get page title based on mode
   */
  getPageTitle(): string {
    return this.isEditMode ? 'Edit Attendance Machine' : 'Register New Attendance Machine';
  }

  /**
   * Get submit button text based on mode
   */
  getSubmitButtonText(): string {
    return this.isEditMode ? 'Update Machine' : 'Register Machine';
  }
}
