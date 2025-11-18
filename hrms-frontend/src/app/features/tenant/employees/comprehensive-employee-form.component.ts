import { Component, signal, inject, OnInit, OnDestroy } from '@angular/core';

import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';

// Material imports (kept for layout)
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';

// Services
import { EmployeeService } from '../../../core/services/employee.service';
import { EmployeeDraftService, SaveDraftRequest } from '../../../core/services/employee-draft.service';
import { DepartmentService, DepartmentDropdownDto } from '../organization/departments/services/department.service';
import { ToastService } from '../../../shared/ui';
import { AddressService } from '../../../services/address.service';
import { DistrictDto, VillageDto } from '../../../models/address.models';
import { SalaryComponentsService, SalaryComponentDto } from '../../../core/services/salary-components.service';

// Custom UI imports
import { UiModule } from '../../../shared/ui/ui.module';
import { Chip, ExpansionPanel, ExpansionPanelGroup } from '../../../shared/ui';

@Component({
  selector: 'app-comprehensive-employee-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterModule,
    ExpansionPanel,
    ExpansionPanelGroup,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    UiModule,
    Chip
  ],
  templateUrl: './comprehensive-employee-form.component.html',
  styleUrls: ['./comprehensive-employee-form.component.scss']
})
export class ComprehensiveEmployeeFormComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private employeeService = inject(EmployeeService);
  private draftService = inject(EmployeeDraftService);
  private departmentService = inject(DepartmentService);
  private addressService = inject(AddressService);
  private salaryComponentsService = inject(SalaryComponentsService);
  private toastService = inject(ToastService);
  private destroy$ = new Subject<void>();

  // Form and state
  employeeForm!: FormGroup;
  draftId = signal<string | null>(null);
  isEditMode = signal(false);
  loading = signal(false);
  saving = signal(false);
  lastSaved = signal<Date | null>(null);
  error = signal<string | null>(null);
  completionPercentage = signal(0);
  departments = signal<DepartmentDropdownDto[]>([]);

  // Address data
  districts = signal<DistrictDto[]>([]);
  villages = signal<VillageDto[]>([]);

  // Salary components
  employeeId: string | null = null;
  employeeSalaryComponents = signal<SalaryComponentDto[]>([]);
  loadingSalaryComponents = signal(false);

  // Auto-save
  private autoSaveSubject$ = new Subject<void>();

  // Accordion state
  personalInfoExpanded = true;

  constructor() {
    this.initializeForm();
    this.setupAutoSave();
    this.setupFormValueChanges();
  }

  ngOnInit(): void {
    const employeeId = this.route.snapshot.paramMap.get('id');
    const draftId = this.route.snapshot.queryParamMap.get('draftId');

    // Load departments and districts for dropdowns
    this.loadDepartments();
    this.loadDistricts();

    if (employeeId) {
      this.employeeId = employeeId;
      this.isEditMode.set(true);
      this.loadEmployee(employeeId);
      this.loadEmployeeSalaryComponents(employeeId);
    } else if (draftId) {
      this.draftId.set(draftId);
      this.loadDraft(draftId);
    } else {
      // New employee - generate draft ID
      this.draftId.set(crypto.randomUUID());
      this.loadFromLocalStorage();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm(): void {
    this.employeeForm = this.fb.group({
      // ===== SECTION 1: PERSONAL INFORMATION =====
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      dateOfBirth: ['', Validators.required],
      gender: ['', Validators.required],
      nationality: ['Mauritian', Validators.required],
      nic: ['', Validators.required], // National Identity Card
      passportNumber: [''],
      phoneNumber: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      alternateEmail: ['', Validators.email],
      // Address fields (Mauritius-compliant)
      district: ['', Validators.required],
      village: [''],
      addressLine1: ['', [Validators.required, Validators.minLength(10)]],
      addressLine2: [''],
      city: [''],
      postalCode: [''],
      region: [''],
      country: ['Mauritius', Validators.required],
      bloodGroup: [''],

      // ===== SECTION 2: EMPLOYMENT DETAILS =====
      employeeCode: ['', Validators.required],
      employeeType: ['Local', Validators.required],
      department: ['', Validators.required],
      designation: ['', Validators.required],
      industrySector: ['', Validators.required],
      workLocation: [''],
      reportingTo: [''],
      joinDate: [new Date(), Validators.required],
      probationPeriodMonths: [3],
      employmentContractType: ['Permanent', Validators.required],
      employmentStatus: ['Active', Validators.required],

      // ===== SECTION 3: COMPENSATION =====
      baseSalary: ['', [Validators.required, Validators.min(0)]],
      paymentFrequency: ['Monthly', Validators.required],
      transportAllowance: [0],
      housingAllowance: [0],
      mealAllowance: [0],
      bankName: [''],
      bankAccountNumber: [''],
      bankBranchCode: [''],

      // ===== SECTION 4: STATUTORY COMPLIANCE (MAURITIUS) =====
      csgNumber: [''], // Contribution Sociale Générale
      nsfNumber: [''], // National Savings Fund
      taxIdNumber: [''],
      prNumber: [''], // PRGF - Portable Retirement Gratuity Fund

      // ===== EXPATRIATE-SPECIFIC FIELDS =====
      passportExpiryDate: [''],
      visaNumber: [''],
      visaExpiryDate: [''],
      workPermitNumber: [''],
      workPermitExpiryDate: [''],
      workPermitType: [''],
      residencePermitNumber: [''],

      // ===== SECTION 5: LEAVE ENTITLEMENTS =====
      annualLeaveDays: [20, [Validators.required, Validators.min(0)]],
      sickLeaveDays: [15, [Validators.required, Validators.min(0)]],
      casualLeaveDays: [5, [Validators.required, Validators.min(0)]],
      carryForwardAllowed: [true],

      // ===== SECTION 6: EMERGENCY CONTACT =====
      emergencyContactName: ['', Validators.required],
      emergencyContactRelation: ['', Validators.required],
      emergencyContactPhone: ['', Validators.required],
      emergencyContactAddress: [''],

      // ===== SECTION 7: QUALIFICATIONS & DOCUMENTS =====
      highestQualification: [''],
      university: [''],
      skills: [''],
      languages: [''],
      // Document file paths (populated via file upload)
      resumeFilePath: [''],
      idCopyFilePath: [''],
      certificatesFilePath: [''],
      contractFilePath: [''],

      // ===== SECTION 8: ADDITIONAL INFORMATION =====
      notes: ['']
    });
  }

  private setupAutoSave(): void {
    // Trigger auto-save every 10 seconds
    this.autoSaveSubject$
      .pipe(
        debounceTime(10000), // 10 seconds
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.saveToDatabase();
      });
  }

  private setupFormValueChanges(): void {
    this.employeeForm.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        // Save to localStorage immediately for instant feedback
        this.saveToLocalStorage();
        // Trigger debounced database save
        this.autoSaveSubject$.next();
        // Update completion percentage
        this.updateCompletionPercentage();
      });

    // Watch nationality field for conditional expatriate fields
    this.employeeForm.get('nationality')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((value) => {
        this.updateExpatriateFieldsValidation(value !== 'Mauritian');
      });
  }

  private updateExpatriateFieldsValidation(isExpatriate: boolean): void {
    const expatFields = [
      'passportNumber',
      'passportExpiryDate',
      'visaNumber',
      'workPermitNumber'
    ];

    expatFields.forEach(field => {
      const control = this.employeeForm.get(field);
      if (control) {
        if (isExpatriate) {
          control.setValidators([Validators.required]);
        } else {
          control.clearValidators();
        }
        control.updateValueAndValidity({ emitEvent: false });
      }
    });
  }

  private updateCompletionPercentage(): void {
    const formValue = this.employeeForm.value;
    let filledFields = 0;
    let totalRequiredFields = 0;

    Object.keys(this.employeeForm.controls).forEach(key => {
      const control = this.employeeForm.get(key);
      if (control?.hasValidator(Validators.required)) {
        totalRequiredFields++;
        if (control.value && control.value !== '') {
          filledFields++;
        }
      }
    });

    const percentage = totalRequiredFields > 0
      ? Math.round((filledFields / totalRequiredFields) * 100)
      : 0;

    this.completionPercentage.set(percentage);
  }

  private saveToLocalStorage(): void {
    const draftId = this.draftId();
    if (draftId) {
      this.draftService.saveToLocalStorage(draftId, this.employeeForm.value);
    }
  }

  private loadFromLocalStorage(): void {
    const draftId = this.draftId();
    if (draftId) {
      const data = this.draftService.loadFromLocalStorage(draftId);
      if (data) {
        this.employeeForm.patchValue(data, { emitEvent: false });
        this.toastService.info('Draft recovered from local storage', 3000);
      }
    }
  }

  private saveToDatabase(): void {
    if (this.saving()) return;

    const draftId = this.draftId();
    if (!draftId) return;

    this.saving.set(true);

    const draftName = this.generateDraftName();
    const request: SaveDraftRequest = {
      id: draftId,
      formDataJson: JSON.stringify(this.employeeForm.value),
      draftName,
      completionPercentage: this.completionPercentage()
    };

    this.draftService.saveDraft(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.lastSaved.set(new Date());
          // Don't show snackbar for auto-save to avoid annoyance
        },
        error: (error) => {
          console.error('Auto-save failed:', error);
          this.saving.set(false);
        }
      });
  }

  private generateDraftName(): string {
    const firstName = this.employeeForm.get('firstName')?.value || 'New';
    const lastName = this.employeeForm.get('lastName')?.value || 'Employee';
    const department = this.employeeForm.get('department')?.value || '';
    return department
      ? `${firstName} ${lastName} - ${department}`
      : `${firstName} ${lastName}`;
  }

  private loadEmployee(id: string): void {
    this.loading.set(true);
    this.employeeService.getEmployeeById(id).subscribe({
      next: (data: any) => {
        const employee = data.data || data;
        this.employeeForm.patchValue(employee, { emitEvent: false });
        this.loading.set(false);
        this.updateCompletionPercentage();
      },
      error: (error) => {
        console.error('Error loading employee:', error);
        this.error.set('Failed to load employee data');
        this.loading.set(false);
      }
    });
  }

  private loadDraft(draftId: string): void {
    this.loading.set(true);
    this.draftService.getDraftById(draftId).subscribe({
      next: (response) => {
        const draft = response.data;
        const formData = JSON.parse(draft.formDataJson);
        this.employeeForm.patchValue(formData, { emitEvent: false });
        this.completionPercentage.set(draft.completionPercentage);
        this.loading.set(false);
        this.toastService.info(`Draft loaded: ${draft.draftName}`, 3000);
      },
      error: (error) => {
        console.error('Error loading draft:', error);
        this.error.set('Failed to load draft');
        this.loading.set(false);
      }
    });
  }

  private loadDepartments(): void {
    this.departmentService.getDropdown()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (depts) => {
          this.departments.set(depts);
        },
        error: (error) => {
          console.error('Error loading departments:', error);
          // Don't block form if departments fail to load
          this.toastService.warning('Warning: Could not load departments', 3000);
        }
      });
  }

  private loadDistricts(): void {
    this.addressService.getDistricts()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (districts) => {
          this.districts.set(districts);
        },
        error: (error) => {
          console.error('Error loading districts:', error);
          this.toastService.warning('Warning: Could not load districts', 3000);
        }
      });
  }

  onDistrictChange(districtId: string): void {
    if (!districtId) {
      this.villages.set([]);
      return;
    }

    this.addressService.getVillagesByDistrict(+districtId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (villages) => {
          this.villages.set(villages);
        },
        error: (error) => {
          console.error('Error loading villages:', error);
          this.toastService.warning('Warning: Could not load villages', 3000);
        }
      });
  }

  onPostalCodeBlur(code: string): void {
    if (!code || code.length < 3) {
      return;
    }

    this.addressService.lookupPostalCode(code)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (results) => {
          if (results.length > 0) {
            const result = results[0];
            // Auto-fill address fields based on postal code lookup
            this.employeeForm.patchValue({
              district: result.districtId.toString(),
              village: result.villageId.toString(),
              city: result.villageName,
              region: result.region
            });
            // Trigger village loading for the selected district
            this.onDistrictChange(result.districtId.toString());
          }
        },
        error: (error) => {
          console.error('Error looking up postal code:', error);
        }
      });
  }

  saveDraft(): void {
    this.saveToDatabase();
    this.toastService.success('Draft saved successfully', 2000);
  }

  onSubmit(): void {
    if (this.employeeForm.invalid) {
      this.toastService.warning('Please fill all required fields', 3000);
      this.markFormGroupTouched(this.employeeForm);
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const employeeData = this.prepareEmployeeData();

    this.employeeService.createEmployee(employeeData).subscribe({
      next: (response: any) => {
        // Clear localStorage draft
        const draftId = this.draftId();
        if (draftId) {
          this.draftService.clearFromLocalStorage(draftId);
        }

        this.toastService.success('Employee created successfully!', 3000);
        this.router.navigate(['/tenant/employees']);
      },
      error: (error) => {
        console.error('Error creating employee:', error);
        this.error.set(error.error?.message || 'Failed to create employee');
        this.loading.set(false);
        this.toastService.error(this.error()!, 5000);
      }
    });
  }

  private prepareEmployeeData(): any {
    const formValue = this.employeeForm.value;
    // Convert dates to ISO format
    return {
      ...formValue,
      dateOfBirth: formValue.dateOfBirth ? new Date(formValue.dateOfBirth).toISOString() : null,
      joinDate: formValue.joinDate ? new Date(formValue.joinDate).toISOString() : null,
      passportExpiryDate: formValue.passportExpiryDate ? new Date(formValue.passportExpiryDate).toISOString() : null,
      visaExpiryDate: formValue.visaExpiryDate ? new Date(formValue.visaExpiryDate).toISOString() : null,
      workPermitExpiryDate: formValue.workPermitExpiryDate ? new Date(formValue.workPermitExpiryDate).toISOString() : null,
    };
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }

  isExpatriate(): boolean {
    return this.employeeForm.get('nationality')?.value !== 'Mauritian';
  }

  cancel(): void {
    if (confirm('Are you sure? Unsaved changes will be lost.')) {
      this.router.navigate(['/tenant/employees']);
    }
  }

  getSaveStatusText(): string {
    if (this.saving()) return 'Saving...';
    if (this.lastSaved()) {
      const now = new Date();
      const diff = Math.floor((now.getTime() - this.lastSaved()!.getTime()) / 1000);
      if (diff < 60) return `Saved ${diff}s ago`;
      if (diff < 3600) return `Saved ${Math.floor(diff / 60)}m ago`;
      return `Saved at ${this.lastSaved()!.toLocaleTimeString()}`;
    }
    return 'Not saved';
  }

  // Salary Components Methods
  private loadEmployeeSalaryComponents(employeeId: string): void {
    this.loadingSalaryComponents.set(true);
    this.salaryComponentsService.getEmployeeComponents(employeeId, true)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (components) => {
          this.employeeSalaryComponents.set(components);
          this.loadingSalaryComponents.set(false);
        },
        error: (error) => {
          console.error('Error loading salary components:', error);
          this.loadingSalaryComponents.set(false);
          this.toastService.error('Failed to load salary components', 3000);
        }
      });
  }

  openSalaryComponentsManagement(): void {
    this.router.navigate(['/tenant/payroll/salary-components'], {
      queryParams: { employeeId: this.employeeId }
    });
  }

  formatCurrency(amount: number): string {
    return `MUR ${amount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }

  getTotalAllowances(): number {
    return this.employeeSalaryComponents()
      .filter(c => c.componentType === 'Allowance' && c.isActive)
      .reduce((sum, c) => sum + c.amount, 0);
  }

  getTotalDeductions(): number {
    return this.employeeSalaryComponents()
      .filter(c => c.componentType === 'Deduction' && c.isActive)
      .reduce((sum, c) => sum + c.amount, 0);
  }

  getNetAdjustment(): number {
    return this.getTotalAllowances() - this.getTotalDeductions();
  }

  // ==================== CUSTOM UI HELPERS ====================

  // Select options for dropdowns
  genderOptions = [
    { value: 'Male', label: 'Male' },
    { value: 'Female', label: 'Female' },
    { value: 'Other', label: 'Other' }
  ];

  nationalityOptions = [
    { value: 'Mauritian', label: 'Mauritian' },
    { value: 'French', label: 'French' },
    { value: 'Indian', label: 'Indian' },
    { value: 'British', label: 'British' },
    { value: 'Other', label: 'Other' }
  ];

  bloodGroupOptions = [
    { value: 'A+', label: 'A+' },
    { value: 'A-', label: 'A-' },
    { value: 'B+', label: 'B+' },
    { value: 'B-', label: 'B-' },
    { value: 'O+', label: 'O+' },
    { value: 'O-', label: 'O-' },
    { value: 'AB+', label: 'AB+' },
    { value: 'AB-', label: 'AB-' }
  ];

  countryOptions = [
    { value: 'Mauritius', label: 'Mauritius' },
    { value: 'Rodrigues', label: 'Rodrigues' },
    { value: 'Agalega', label: 'Agalega' }
  ];

  employeeTypeOptions = [
    { value: 'Local', label: 'Local' },
    { value: 'Expatriate', label: 'Expatriate' }
  ];

  industrySectorOptions = [
    { value: 'Manufacturing', label: 'Manufacturing' },
    { value: 'EPZ', label: 'EPZ (Export Processing Zone)' },
    { value: 'Tourism', label: 'Tourism & Hospitality' },
    { value: 'Financial Services', label: 'Financial Services' },
    { value: 'ICT', label: 'ICT & Technology' },
    { value: 'Construction', label: 'Construction' },
    { value: 'Retail', label: 'Retail & Trade' },
    { value: 'Agriculture', label: 'Agriculture & Fishing' }
  ];

  employmentContractTypeOptions = [
    { value: 'Permanent', label: 'Permanent' },
    { value: 'Fixed-Term', label: 'Fixed-Term' },
    { value: 'Casual', label: 'Casual' },
    { value: 'Contract', label: 'Contract' }
  ];

  employmentStatusOptions = [
    { value: 'Active', label: 'Active' },
    { value: 'On Probation', label: 'On Probation' },
    { value: 'On Leave', label: 'On Leave' },
    { value: 'Suspended', label: 'Suspended' },
    { value: 'Terminated', label: 'Terminated' }
  ];

  paymentFrequencyOptions = [
    { value: 'Monthly', label: 'Monthly' },
    { value: 'Bi-weekly', label: 'Bi-weekly' },
    { value: 'Weekly', label: 'Weekly' },
    { value: 'Daily', label: 'Daily' }
  ];

  emergencyContactRelationOptions = [
    { value: 'Spouse', label: 'Spouse' },
    { value: 'Parent', label: 'Parent' },
    { value: 'Sibling', label: 'Sibling' },
    { value: 'Child', label: 'Child' },
    { value: 'Friend', label: 'Friend' },
    { value: 'Other', label: 'Other' }
  ];

  highestQualificationOptions = [
    { value: 'High School', label: 'High School' },
    { value: 'Diploma', label: 'Diploma' },
    { value: "Bachelor's Degree", label: "Bachelor's Degree" },
    { value: "Master's Degree", label: "Master's Degree" },
    { value: 'PhD', label: 'PhD' },
    { value: 'Professional Certificate', label: 'Professional Certificate' }
  ];

  workPermitTypeOptions = [
    { value: 'Occupation Permit', label: 'Occupation Permit' },
    { value: 'Residence Permit', label: 'Residence Permit' },
    { value: 'Work Permit', label: 'Work Permit' }
  ];

  // Convert districts to select options
  get districtOptions() {
    return this.districts().map(d => ({
      value: d.id.toString(),
      label: d.districtName
    }));
  }

  // Convert villages to select options
  get villageOptions() {
    return this.villages().map(v => ({
      value: v.id.toString(),
      label: `${v.villageName} (${v.postalCode})`
    }));
  }

  // Convert departments to select options
  get departmentOptions() {
    return this.departments().map(d => ({
      value: d.name,
      label: `${d.name} (${d.code})`
    }));
  }

  // Error helper for form validation
  getFieldError(fieldName: string): string | null {
    const control = this.employeeForm.get(fieldName);
    if (control && control.invalid && (control.dirty || control.touched)) {
      if (control.hasError('required')) {
        return `${this.getFieldLabel(fieldName)} is required`;
      }
      if (control.hasError('email')) {
        return 'Please enter a valid email';
      }
      if (control.hasError('minlength')) {
        const minLength = control.getError('minlength').requiredLength;
        return `Must be at least ${minLength} characters`;
      }
      if (control.hasError('min')) {
        const min = control.getError('min').min;
        return `Must be at least ${min}`;
      }
    }
    return null;
  }

  private getFieldLabel(fieldName: string): string {
    const labels: Record<string, string> = {
      'firstName': 'First name',
      'lastName': 'Last name',
      'dateOfBirth': 'Date of birth',
      'gender': 'Gender',
      'nationality': 'Nationality',
      'nic': 'NIC',
      'phoneNumber': 'Phone number',
      'email': 'Email',
      'alternateEmail': 'Alternate email',
      'district': 'District',
      'addressLine1': 'Address line 1',
      'country': 'Country',
      'employeeCode': 'Employee code',
      'employeeType': 'Employee type',
      'department': 'Department',
      'designation': 'Designation',
      'industrySector': 'Industry sector',
      'joinDate': 'Join date',
      'employmentContractType': 'Employment contract type',
      'employmentStatus': 'Employment status',
      'baseSalary': 'Base salary',
      'paymentFrequency': 'Payment frequency',
      'annualLeaveDays': 'Annual leave days',
      'sickLeaveDays': 'Sick leave days',
      'casualLeaveDays': 'Casual leave days',
      'emergencyContactName': 'Emergency contact name',
      'emergencyContactRelation': 'Emergency contact relation',
      'emergencyContactPhone': 'Emergency contact phone',
      'passportNumber': 'Passport number',
      'passportExpiryDate': 'Passport expiry date',
      'visaNumber': 'Visa number',
      'workPermitNumber': 'Work permit number'
    };
    return labels[fieldName] || fieldName;
  }
}
