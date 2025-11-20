// ═══════════════════════════════════════════════════════════════
// MAURITIUS VALIDATORS - Production Grade
// Country-specific form validators (NIC, phone, postal code)
// NO AI - Pure regex validation
// Angular Reactive Forms compatible
// ═══════════════════════════════════════════════════════════════

import { AbstractControl, ValidationErrors, ValidatorFn, AsyncValidatorFn } from '@angular/forms';
import { Observable, of, timer } from 'rxjs';
import { map, switchMap, catchError } from 'rxjs/operators';
import { inject } from '@angular/core';
import { EmployeeService } from '../services/employee.service';

/**
 * Validate Mauritius NIC (National Identity Card) format
 * Format: Single letter followed by 14 digits (e.g., A12345678901234)
 * or 2 letters followed by 6 digits (old format)
 */
export function mauritiusNICValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null; // Don't validate empty values
    }

    const value = control.value.toString().trim().toUpperCase();

    // New format: 1 letter + 14 digits
    const newFormat = /^[A-Z]\d{14}$/;
    // Old format: 1-2 letters + 6-13 digits
    const oldFormat = /^[A-Z]{1,2}\d{6,13}$/;

    if (newFormat.test(value) || oldFormat.test(value)) {
      return null; // Valid
    }

    return {
      mauritiusNIC: {
        valid: false,
        message: 'Invalid NIC format. Expected format: A12345678901234 (1 letter + 14 digits)'
      }
    };
  };
}

/**
 * Validate Mauritius phone number
 * Formats accepted:
 * - Mobile: +230 5XXX XXXX (5, 5, 9 series)
 * - Landline: +230 XXX XXXX
 * - International: +230XXXXXXXX
 */
export function mauritiusPhoneValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const value = control.value.toString().trim().replace(/\s/g, '');

    // Remove +230 prefix if present
    const normalized = value.startsWith('+230')
      ? value.substring(4)
      : value.startsWith('230')
      ? value.substring(3)
      : value;

    // Mobile: 5XXX XXXX (8 digits starting with 5)
    const mobilePattern = /^5\d{7}$/;
    // Landline: XXX XXXX (7 digits, not starting with 5)
    const landlinePattern = /^[2-4,6-9]\d{6}$/;

    if (mobilePattern.test(normalized) || landlinePattern.test(normalized)) {
      return null; // Valid
    }

    return {
      mauritiusPhone: {
        valid: false,
        message: 'Invalid Mauritius phone number. Expected: +230 5XXX XXXX (mobile) or +230 XXX XXXX (landline)'
      }
    };
  };
}

/**
 * Validate Mauritius postal code
 * Format: 5 digits (e.g., 11302)
 */
export function mauritiusPostalCodeValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const value = control.value.toString().trim();
    const pattern = /^\d{5}$/;

    if (pattern.test(value)) {
      return null; // Valid
    }

    return {
      mauritiusPostalCode: {
        valid: false,
        message: 'Invalid postal code. Must be 5 digits (e.g., 11302)'
      }
    };
  };
}

/**
 * Validate salary meets minimum for permit type
 */
export function minimumSalaryValidator(minimumSalary: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const salary = parseFloat(control.value);

    if (isNaN(salary) || salary < minimumSalary) {
      return {
        minimumSalary: {
          valid: false,
          required: minimumSalary,
          actual: salary,
          message: `Salary must be at least MUR ${minimumSalary.toLocaleString()}`
        }
      };
    }

    return null;
  };
}

/**
 * Validate passport expiry (must be valid for at least 6 months)
 */
export function passportExpiryValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const expiryDate = new Date(control.value);
    const today = new Date();
    const sixMonthsFromNow = new Date();
    sixMonthsFromNow.setMonth(sixMonthsFromNow.getMonth() + 6);

    if (expiryDate < today) {
      return {
        passportExpiry: {
          valid: false,
          message: 'Passport has expired'
        }
      };
    }

    if (expiryDate < sixMonthsFromNow) {
      return {
        passportExpiry: {
          valid: false,
          message: 'Passport must be valid for at least 6 months'
        }
      };
    }

    return null;
  };
}

/**
 * Async validator: Check if employee code is unique
 * Uses debouncing to avoid excessive API calls
 */
export function uniqueEmployeeCodeValidator(
  employeeService: EmployeeService,
  currentEmployeeId?: string
): AsyncValidatorFn {
  return (control: AbstractControl): Observable<ValidationErrors | null> => {
    if (!control.value) {
      return of(null);
    }

    // Debounce for 500ms to avoid API spam
    return timer(500).pipe(
      switchMap(() =>
        employeeService.checkEmployeeCodeExists(control.value, currentEmployeeId)
      ),
      map((exists: boolean) =>
        exists
          ? {
              uniqueEmployeeCode: {
                valid: false,
                message: 'Employee code already exists. Please use a unique code.'
              }
            }
          : null
      ),
      catchError(() => of(null)) // On error, pass validation
    );
  };
}

/**
 * Validate date is in the past (for date of birth, join date history)
 */
export function pastDateValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const date = new Date(control.value);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    if (date >= today) {
      return {
        pastDate: {
          valid: false,
          message: 'Date must be in the past'
        }
      };
    }

    return null;
  };
}

/**
 * Validate date is in the future (for expiry dates)
 */
export function futureDateValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const date = new Date(control.value);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    if (date <= today) {
      return {
        futureDate: {
          valid: false,
          message: 'Date must be in the future'
        }
      };
    }

    return null;
  };
}

/**
 * Validate age is within range (for employment)
 * Mauritius: Minimum working age is 16, retirement age is 65
 */
export function ageRangeValidator(minAge: number = 16, maxAge: number = 65): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const birthDate = new Date(control.value);
    const today = new Date();
    const age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();

    const actualAge = monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())
      ? age - 1
      : age;

    if (actualAge < minAge) {
      return {
        ageRange: {
          valid: false,
          message: `Employee must be at least ${minAge} years old (current age: ${actualAge})`
        }
      };
    }

    if (actualAge > maxAge) {
      return {
        ageRange: {
          valid: false,
          message: `Employee must be under ${maxAge} years old (current age: ${actualAge})`
        }
      };
    }

    return null;
  };
}

/**
 * Conditional validator: Require field if condition is met
 * Example: Require passport if nationality is not Mauritian
 */
export function conditionalRequiredValidator(
  conditionFn: () => boolean,
  message: string = 'This field is required'
): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!conditionFn()) {
      return null; // Condition not met, don't validate
    }

    if (!control.value || control.value.toString().trim() === '') {
      return {
        conditionalRequired: {
          valid: false,
          message
        }
      };
    }

    return null;
  };
}
