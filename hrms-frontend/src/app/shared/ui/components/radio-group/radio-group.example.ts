// ═══════════════════════════════════════════════════════════
// RADIO GROUP COMPONENT EXAMPLES
// Demonstrates various usage patterns for the RadioGroup component
// ═══════════════════════════════════════════════════════════

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RadioGroup, RadioOption } from './radio-group';
import { Radio } from '../radio/radio';

// ───────────────────────────────────────────────────────────
// Example 1: Basic Vertical Radio Group
// ───────────────────────────────────────────────────────────
@Component({
  selector: 'app-basic-radio-example',
  standalone: true,
  imports: [CommonModule, RadioGroup],
  template: `
    <div class="example-container">
      <h3>Basic Radio Group</h3>
      <app-radio-group
        label="Select your preferred language"
        [options]="languageOptions"
        [value]="selectedLanguage"
        [required]="true"
        (valueChange)="onLanguageChange($event)">
      </app-radio-group>
      <p class="selected-value" *ngIf="selectedLanguage">
        Selected: {{ selectedLanguage }}
      </p>
    </div>
  `,
  styles: [`
    .example-container {
      padding: 24px;
      max-width: 400px;
    }
    .selected-value {
      margin-top: 16px;
      padding: 12px;
      background-color: var(--gray-100);
      border-radius: 8px;
      font-size: 14px;
    }
  `]
})
export class BasicRadioExample {
  languageOptions: RadioOption[] = [
    { label: 'English', value: 'en' },
    { label: 'Spanish', value: 'es' },
    { label: 'French', value: 'fr' },
    { label: 'German', value: 'de' }
  ];

  selectedLanguage: string = 'en';

  onLanguageChange(value: string): void {
    console.log('Language changed to:', value);
    this.selectedLanguage = value;
  }
}

// ───────────────────────────────────────────────────────────
// Example 2: Horizontal Radio Group
// ───────────────────────────────────────────────────────────
@Component({
  selector: 'app-horizontal-radio-example',
  standalone: true,
  imports: [CommonModule, RadioGroup],
  template: `
    <div class="example-container">
      <h3>Horizontal Layout</h3>
      <app-radio-group
        label="Choose your plan"
        [options]="planOptions"
        [value]="selectedPlan"
        layout="horizontal"
        (valueChange)="onPlanChange($event)">
      </app-radio-group>
      <div class="plan-details" *ngIf="selectedPlan">
        <strong>{{ getPlanName(selectedPlan) }}</strong> plan selected
      </div>
    </div>
  `,
  styles: [`
    .example-container {
      padding: 24px;
    }
    .plan-details {
      margin-top: 16px;
      padding: 12px;
      background-color: var(--primary-50);
      border-radius: 8px;
      color: var(--primary-700);
    }
  `]
})
export class HorizontalRadioExample {
  planOptions: RadioOption[] = [
    { label: 'Basic', value: 'basic' },
    { label: 'Pro', value: 'pro' },
    { label: 'Enterprise', value: 'enterprise' }
  ];

  selectedPlan: string = 'basic';

  onPlanChange(value: string): void {
    this.selectedPlan = value;
  }

  getPlanName(value: string): string {
    const plan = this.planOptions.find(p => p.value === value);
    return plan ? plan.label : '';
  }
}

// ───────────────────────────────────────────────────────────
// Example 3: Radio Group with Disabled Options
// ───────────────────────────────────────────────────────────
@Component({
  selector: 'app-disabled-radio-example',
  standalone: true,
  imports: [CommonModule, RadioGroup],
  template: `
    <div class="example-container">
      <h3>Radio Group with Disabled Options</h3>
      <app-radio-group
        label="Select a payment method"
        [options]="paymentOptions"
        [value]="selectedPayment"
        (valueChange)="onPaymentChange($event)">
      </app-radio-group>
    </div>
  `,
  styles: [`
    .example-container {
      padding: 24px;
      max-width: 400px;
    }
  `]
})
export class DisabledRadioExample {
  paymentOptions: RadioOption[] = [
    { label: 'Credit Card', value: 'credit' },
    { label: 'PayPal', value: 'paypal' },
    { label: 'Bank Transfer', value: 'bank', disabled: true },
    { label: 'Cryptocurrency', value: 'crypto', disabled: true }
  ];

  selectedPayment: string = 'credit';

  onPaymentChange(value: string): void {
    this.selectedPayment = value;
  }
}

// ───────────────────────────────────────────────────────────
// Example 4: Individual Radio Buttons
// ───────────────────────────────────────────────────────────
@Component({
  selector: 'app-individual-radio-example',
  standalone: true,
  imports: [CommonModule, Radio],
  template: `
    <div class="example-container">
      <h3>Individual Radio Buttons</h3>
      <div class="radio-list">
        <app-radio
          label="I agree to the terms and conditions"
          [value]="true"
          [checked]="agreedToTerms"
          (change)="onTermsChange($event)">
        </app-radio>

        <app-radio
          label="I want to receive marketing emails"
          [value]="true"
          [checked]="marketingEmails"
          (change)="onMarketingChange($event)">
        </app-radio>

        <app-radio
          label="Enable two-factor authentication"
          [value]="true"
          [checked]="twoFactorAuth"
          [disabled]="!agreedToTerms"
          (change)="onTwoFactorChange($event)">
        </app-radio>
      </div>
    </div>
  `,
  styles: [`
    .example-container {
      padding: 24px;
      max-width: 500px;
    }
    .radio-list {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }
  `]
})
export class IndividualRadioExample {
  agreedToTerms: boolean = false;
  marketingEmails: boolean = false;
  twoFactorAuth: boolean = false;

  onTermsChange(value: boolean): void {
    this.agreedToTerms = value;
    if (!value) {
      this.twoFactorAuth = false;
    }
  }

  onMarketingChange(value: boolean): void {
    this.marketingEmails = value;
  }

  onTwoFactorChange(value: boolean): void {
    this.twoFactorAuth = value;
  }
}

// ───────────────────────────────────────────────────────────
// Example 5: Dynamic Radio Group
// ───────────────────────────────────────────────────────────
@Component({
  selector: 'app-dynamic-radio-example',
  standalone: true,
  imports: [CommonModule, RadioGroup],
  template: `
    <div class="example-container">
      <h3>Dynamic Radio Group</h3>
      <app-radio-group
        label="Select a country"
        [options]="countryOptions"
        [value]="selectedCountry"
        (valueChange)="onCountryChange($event)">
      </app-radio-group>

      <app-radio-group
        *ngIf="cityOptions.length > 0"
        label="Select a city"
        [options]="cityOptions"
        [value]="selectedCity"
        (valueChange)="onCityChange($event)">
      </app-radio-group>
    </div>
  `,
  styles: [`
    .example-container {
      padding: 24px;
      max-width: 400px;
      display: flex;
      flex-direction: column;
      gap: 24px;
    }
  `]
})
export class DynamicRadioExample {
  countryOptions: RadioOption[] = [
    { label: 'United States', value: 'us' },
    { label: 'Canada', value: 'ca' },
    { label: 'United Kingdom', value: 'uk' }
  ];

  cityOptions: RadioOption[] = [];
  selectedCountry: string = '';
  selectedCity: string = '';

  private cityData: Record<string, RadioOption[]> = {
    'us': [
      { label: 'New York', value: 'ny' },
      { label: 'Los Angeles', value: 'la' },
      { label: 'Chicago', value: 'chi' }
    ],
    'ca': [
      { label: 'Toronto', value: 'tor' },
      { label: 'Vancouver', value: 'van' },
      { label: 'Montreal', value: 'mtl' }
    ],
    'uk': [
      { label: 'London', value: 'lon' },
      { label: 'Manchester', value: 'man' },
      { label: 'Edinburgh', value: 'edi' }
    ]
  };

  onCountryChange(value: string): void {
    this.selectedCountry = value;
    this.cityOptions = this.cityData[value] || [];
    this.selectedCity = '';
  }

  onCityChange(value: string): void {
    this.selectedCity = value;
  }
}
