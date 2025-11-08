import { Component, OnInit } from '@angular/core';
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
import { LocationService, LocationDropdownDto } from '../locations/location.service';
import { BiometricDeviceService, CreateBiometricDeviceDto, UpdateBiometricDeviceDto, TestConnectionDto } from './biometric-device.service';

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
    MatTooltipModule
  ],
  templateUrl: './biometric-device-form.component.html',
  styleUrls: ['./biometric-device-form.component.scss']
})
export class BiometricDeviceFormComponent implements OnInit {
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
    private deviceService: BiometricDeviceService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadLocations();
    this.checkEditMode();
    this.setupFormListeners();
  }

  /**
   * Initialize reactive form with validators
   */
  private initForm(): void {
    this.deviceForm = this.fb.group({
      // Device Information Section
      deviceCode: ['', [Validators.required, Validators.maxLength(50)]],
      machineName: ['', [Validators.required, Validators.maxLength(100)]],
      locationId: ['', Validators.required],
      deviceType: ['ZKTeco', Validators.required],
      model: ['', Validators.maxLength(100)],

      // Network Configuration Section
      connectionMethod: ['TCP/IP', Validators.required],
      ipAddress: [
        '',
        [
          Validators.required,
          Validators.pattern(this.ipAddressPattern),
          this.ipAddressValidator.bind(this)
        ]
      ],
      port: [
        4370,
        [
          Validators.required,
          Validators.min(1),
          Validators.max(65535)
        ]
      ],
      macAddress: [
        '',
        [Validators.pattern(this.macAddressPattern)]
      ],

      // Device Credentials Section (Optional)
      username: ['', Validators.maxLength(50)],
      password: ['', Validators.maxLength(100)],

      // Sync Settings Section
      syncEnabled: [true],
      syncIntervalMinutes: [
        15,
        [
          Validators.required,
          Validators.min(1),
          Validators.max(1440)
        ]
      ],
      connectionTimeoutSeconds: [
        30,
        [
          Validators.required,
          Validators.min(5),
          Validators.max(300)
        ]
      ],
      offlineAlertEnabled: [true],
      offlineThresholdMinutes: [
        60,
        [
          Validators.required,
          Validators.min(5),
          Validators.max(1440)
        ]
      ],

      // Device Identification Section (Optional)
      serialNumber: ['', Validators.maxLength(100)],
      firmwareVersion: ['', Validators.maxLength(50)],

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
    // Enable/disable IP and Port based on connection method
    this.deviceForm.get('connectionMethod')?.valueChanges.subscribe(method => {
      const ipControl = this.deviceForm.get('ipAddress');
      const portControl = this.deviceForm.get('port');

      if (method === 'TCP/IP' || method === 'WiFi') {
        ipControl?.setValidators([
          Validators.required,
          Validators.pattern(this.ipAddressPattern),
          this.ipAddressValidator.bind(this)
        ]);
        portControl?.setValidators([
          Validators.required,
          Validators.min(1),
          Validators.max(65535)
        ]);
      } else {
        ipControl?.clearValidators();
        portControl?.clearValidators();
      }

      ipControl?.updateValueAndValidity();
      portControl?.updateValueAndValidity();
    });

    // Enable/disable sync interval based on sync enabled
    this.deviceForm.get('syncEnabled')?.valueChanges.subscribe(enabled => {
      const intervalControl = this.deviceForm.get('syncIntervalMinutes');

      if (enabled) {
        intervalControl?.enable();
      } else {
        intervalControl?.disable();
      }
    });

    // Enable/disable offline threshold based on alert enabled
    this.deviceForm.get('offlineAlertEnabled')?.valueChanges.subscribe(enabled => {
      const thresholdControl = this.deviceForm.get('offlineThresholdMinutes');

      if (enabled) {
        thresholdControl?.enable();
      } else {
        thresholdControl?.disable();
      }
    });
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
   * Load existing device data for editing
   */
  private loadDeviceData(id: string): void {
    this.loading = true;

    this.deviceService.getDevice(id).subscribe({
      next: (device) => {
        this.deviceForm.patchValue({
          deviceCode: device.deviceCode,
          machineName: device.machineName,
          locationId: device.locationId,
          deviceType: device.deviceType,
          model: device.model,
          connectionMethod: device.connectionMethod,
          ipAddress: device.ipAddress,
          port: device.port,
          macAddress: device.macAddress,
          username: '',
          password: '',
          syncEnabled: device.syncEnabled,
          syncIntervalMinutes: device.syncIntervalMinutes,
          connectionTimeoutSeconds: device.connectionTimeoutSeconds,
          offlineAlertEnabled: device.offlineAlertEnabled,
          offlineThresholdMinutes: device.offlineThresholdMinutes,
          serialNumber: device.serialNumber,
          firmwareVersion: device.firmwareVersion,
          isActive: device.isActive
        });
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading device:', error);
        this.snackBar.open('Failed to load device data', 'Close', { duration: 3000 });
        this.loading = false;
      }
    });
  }

  /**
   * Test device connection
   */
  onTestConnection(): void {
    // Validate required network fields
    const ipAddress = this.deviceForm.get('ipAddress')?.value;
    const port = this.deviceForm.get('port')?.value;
    const connectionMethod = this.deviceForm.get('connectionMethod')?.value;
    const connectionTimeoutSeconds = this.deviceForm.get('connectionTimeoutSeconds')?.value;

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
      connectionMethod,
      connectionTimeoutSeconds
    };

    this.deviceService.testConnection(connectionData).subscribe({
      next: (result) => {
        this.testingConnection = false;
        if (result.success) {
          this.snackBar.open(
            `✓ Successfully connected to device at ${ipAddress}:${port}`,
            'Close',
            {
              duration: 5000,
              panelClass: ['success-snackbar']
            }
          );
        } else {
          this.snackBar.open(
            `✗ Failed to connect to device at ${ipAddress}:${port}. ${result.message}`,
            'Close',
            {
              duration: 6000,
              panelClass: ['error-snackbar']
            }
          );
        }
      },
      error: (error) => {
        this.testingConnection = false;
        console.error('Connection test error:', error);
        this.snackBar.open(
          `✗ Failed to connect to device at ${ipAddress}:${port}. Please check IP, port, and network settings.`,
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
   * Submit form to create or update device
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

    // Prepare device DTO
    const deviceDto = {
      deviceCode: formData.deviceCode,
      machineName: formData.machineName,
      machineId: formData.deviceCode, // Use device code as machine ID
      deviceType: formData.deviceType,
      model: formData.model,
      locationId: formData.locationId,
      ipAddress: formData.ipAddress,
      port: formData.port,
      macAddress: formData.macAddress,
      serialNumber: formData.serialNumber,
      firmwareVersion: formData.firmwareVersion,
      syncEnabled: formData.syncEnabled,
      syncIntervalMinutes: formData.syncIntervalMinutes,
      connectionMethod: formData.connectionMethod,
      connectionTimeoutSeconds: formData.connectionTimeoutSeconds,
      offlineAlertEnabled: formData.offlineAlertEnabled,
      offlineThresholdMinutes: formData.offlineThresholdMinutes,
      isActive: formData.isActive
    };

    const saveOperation = this.isEditMode
      ? this.deviceService.updateDevice(this.deviceId!, deviceDto as UpdateBiometricDeviceDto)
      : this.deviceService.createDevice(deviceDto as CreateBiometricDeviceDto);

    saveOperation.subscribe({
      next: () => {
        this.savingDevice = false;
        const action = this.isEditMode ? 'updated' : 'created';
        this.snackBar.open(
          `✓ Biometric device ${action} successfully`,
          'Close',
          {
            duration: 3000,
            panelClass: ['success-snackbar']
          }
        );
        this.router.navigate(['/tenant/organization/devices']);
      },
      error: (error) => {
        this.savingDevice = false;
        console.error('Save error:', error);
        this.snackBar.open(
          `Failed to ${this.isEditMode ? 'update' : 'create'} device`,
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
   * Check if network fields should be enabled
   */
  isNetworkConfigRequired(): boolean {
    const method = this.deviceForm.get('connectionMethod')?.value;
    return method === 'TCP/IP' || method === 'WiFi';
  }

  /**
   * Get page title based on mode
   */
  getPageTitle(): string {
    return this.isEditMode ? 'Edit Biometric Device' : 'Register New Biometric Device';
  }

  /**
   * Get submit button text based on mode
   */
  getSubmitButtonText(): string {
    return this.isEditMode ? 'Update Device' : 'Register Device';
  }
}
