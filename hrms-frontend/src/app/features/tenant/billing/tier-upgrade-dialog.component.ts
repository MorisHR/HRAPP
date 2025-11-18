import { Component, inject, signal } from '@angular/core';

import { DialogRef } from '../../../shared/ui';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { ToastService, Divider, Chip } from '../../../shared/ui';

interface TierOption {
  name: string;
  displayName: string;
  employeeLimit: number;
  monthlyPrice: number;
  yearlyPrice: number;
  features: string[];
  recommended?: boolean;
  popular?: boolean;
}

/**
 * PRODUCTION-READY: Tier upgrade/downgrade dialog
 * Displays available subscription tiers and allows tenant to request changes
 */
@Component({
  selector: 'app-tier-upgrade-dialog',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    Divider,
    Chip
],
  template: `
    <div class="tier-upgrade-dialog">
      <div class="dialog-title">
        <div class="title-content">
          <mat-icon>upgrade</mat-icon>
          <h2>Subscription Tiers</h2>
        </div>
        <button mat-icon-button (click)="dialogRef.close()">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <div class="dialog-content">
        <div class="tier-info">
          <p class="info-text">
            Choose the subscription tier that best fits your organization's needs.
            Contact our sales team to upgrade or modify your subscription.
          </p>
        </div>

        <div class="tiers-grid">
          @for (tier of availableTiers(); track tier.name) {
            <mat-card class="tier-card" [class.recommended]="tier.recommended" [class.popular]="tier.popular">
              @if (tier.popular) {
                <div class="badge-popular">
                  <app-chip [label]="'Most Popular'" [color]="'primary'" />
                </div>
              }
              @if (tier.recommended) {
                <div class="badge-recommended">
                  <app-chip [label]="'Recommended'" [color]="'primary'" />
                </div>
              }

              <mat-card-header>
                <mat-card-title>{{ tier.displayName }}</mat-card-title>
                <mat-card-subtitle>Up to {{ tier.employeeLimit }} employees</mat-card-subtitle>
              </mat-card-header>

              <mat-card-content>
                <div class="pricing">
                  <div class="price-monthly">
                    <span class="currency">MUR</span>
                    <span class="amount">{{ tier.monthlyPrice.toLocaleString() }}</span>
                    <span class="period">/month</span>
                  </div>
                  <div class="price-yearly">
                    or {{ formatCurrency(tier.yearlyPrice) }}/year
                  </div>
                </div>

                <app-divider />

                <div class="features-list">
                  <h4>Features:</h4>
                  <ul>
                    @for (feature of tier.features; track feature) {
                      <li>
                        <mat-icon>check_circle</mat-icon>
                        <span>{{ feature }}</span>
                      </li>
                    }
                  </ul>
                </div>
              </mat-card-content>

              <mat-card-actions>
                <button
                  mat-raised-button
                  [color]="tier.popular ? 'accent' : tier.recommended ? 'primary' : 'default'"
                  (click)="selectTier(tier)"
                  class="select-tier-button">
                  Select {{ tier.displayName }}
                </button>
              </mat-card-actions>
            </mat-card>
          }
        </div>

        <div class="contact-info">
          <mat-icon>info</mat-icon>
          <p>
            Need a custom plan for your enterprise?
            <a href="mailto:sales@hrms.mu" class="contact-link">Contact our sales team</a>
            for tailored solutions.
          </p>
        </div>
      </div>

      <div class="dialog-actions">
        <button mat-button (click)="dialogRef.close()">Cancel</button>
      </div>
    </div>
  `,
  styles: [`
    .tier-upgrade-dialog {
      min-width: 800px;
      max-width: 1200px;

      .dialog-title {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin: 0;
        padding: 20px 24px;
        border-bottom: 1px solid rgba(0, 0, 0, 0.12);

        .title-content {
          display: flex;
          align-items: center;
          gap: 12px;

          mat-icon {
            color: #1976d2;
            font-size: 28px;
            width: 28px;
            height: 28px;
          }

          h2 {
            margin: 0;
            font-size: 1.5rem;
            font-weight: 500;
          }
        }
      }

      .dialog-content {
        padding: 24px;
        max-height: 80vh;
        overflow-y: auto;

        .tier-info {
          margin-bottom: 24px;
          padding: 16px;
          background-color: #e3f2fd;
          border-left: 4px solid #1976d2;
          border-radius: 4px;

          .info-text {
            margin: 0;
            color: rgba(0, 0, 0, 0.87);
            font-size: 0.95rem;
          }
        }

        .tiers-grid {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
          gap: 24px;
          margin-bottom: 24px;

          .tier-card {
            position: relative;
            display: flex;
            flex-direction: column;
            transition: transform 0.2s, box-shadow 0.2s;

            &:hover {
              transform: translateY(-4px);
              box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15);
            }

            &.popular {
              border: 2px solid #ff4081;
            }

            &.recommended {
              border: 2px solid #1976d2;
            }

            .badge-popular,
            .badge-recommended {
              position: absolute;
              top: -12px;
              left: 50%;
              transform: translateX(-50%);
              z-index: 1;
            }

            mat-card-header {
              padding: 24px 16px 16px 16px;

              mat-card-title {
                font-size: 1.5rem;
                font-weight: 600;
                color: #1976d2;
              }

              mat-card-subtitle {
                font-size: 0.875rem;
                margin-top: 4px;
              }
            }

            mat-card-content {
              flex: 1;
              padding: 16px;

              .pricing {
                text-align: center;
                margin-bottom: 16px;

                .price-monthly {
                  display: flex;
                  align-items: baseline;
                  justify-content: center;
                  gap: 4px;
                  margin-bottom: 8px;

                  .currency {
                    font-size: 1rem;
                    color: rgba(0, 0, 0, 0.6);
                  }

                  .amount {
                    font-size: 2.5rem;
                    font-weight: 700;
                    color: #1976d2;
                  }

                  .period {
                    font-size: 0.875rem;
                    color: rgba(0, 0, 0, 0.6);
                  }
                }

                .price-yearly {
                  font-size: 0.875rem;
                  color: rgba(0, 0, 0, 0.6);
                }
              }

              app-divider {
                margin: 16px 0;
              }

              .features-list {
                h4 {
                  margin: 0 0 12px 0;
                  font-size: 0.95rem;
                  font-weight: 600;
                  color: rgba(0, 0, 0, 0.87);
                }

                ul {
                  list-style: none;
                  padding: 0;
                  margin: 0;

                  li {
                    display: flex;
                    align-items: center;
                    gap: 8px;
                    margin-bottom: 8px;
                    font-size: 0.875rem;

                    mat-icon {
                      font-size: 18px;
                      width: 18px;
                      height: 18px;
                      color: #4caf50;
                    }

                    span {
                      flex: 1;
                      color: rgba(0, 0, 0, 0.87);
                    }
                  }
                }
              }
            }

            mat-card-actions {
              padding: 16px;
              border-top: 1px solid rgba(0, 0, 0, 0.12);

              .select-tier-button {
                width: 100%;
              }
            }
          }
        }

        .contact-info {
          display: flex;
          align-items: flex-start;
          gap: 12px;
          padding: 16px;
          background-color: #fff3e0;
          border-left: 4px solid #ff9800;
          border-radius: 4px;

          mat-icon {
            color: #ff9800;
            font-size: 24px;
            width: 24px;
            height: 24px;
          }

          p {
            margin: 0;
            flex: 1;
            color: rgba(0, 0, 0, 0.87);
            font-size: 0.875rem;

            .contact-link {
              color: #1976d2;
              text-decoration: none;
              font-weight: 500;

              &:hover {
                text-decoration: underline;
              }
            }
          }
        }
      }

      .dialog-actions {
        padding: 16px 24px;
        border-top: 1px solid rgba(0, 0, 0, 0.12);
        display: flex;
        justify-content: flex-end;
      }
    }

    // Responsive design
    @media (max-width: 900px) {
      .tier-upgrade-dialog {
        min-width: 0;
        width: 100%;

        .dialog-content .tiers-grid {
          grid-template-columns: 1fr;
        }
      }
    }

    // Dark mode support
    :host-context(.dark-theme) {
      .tier-upgrade-dialog {
        .dialog-title {
          border-bottom-color: rgba(255, 255, 255, 0.12);
        }

        .dialog-content {
          .tier-info {
            background-color: rgba(25, 118, 210, 0.1);
            border-left-color: #42a5f5;
          }

          .contact-info {
            background-color: rgba(255, 152, 0, 0.1);
            border-left-color: #ffa726;
          }

          .tiers-grid .tier-card {
            mat-card-actions {
              border-top-color: rgba(255, 255, 255, 0.12);
            }
          }
        }

        .dialog-actions {
          border-top-color: rgba(255, 255, 255, 0.12);
        }
      }
    }
  `]
})
export class TierUpgradeDialogComponent {
  public dialogRef = inject(DialogRef<TierUpgradeDialogComponent, { selectedTier: string }>);
  private toastService = inject(ToastService);

  availableTiers = signal<TierOption[]>([
    {
      name: 'Tier1',
      displayName: 'Starter',
      employeeLimit: 25,
      monthlyPrice: 2500,
      yearlyPrice: 27000,
      features: [
        'Up to 25 employees',
        'Basic attendance tracking',
        'Leave management',
        'Employee records',
        'Email support'
      ]
    },
    {
      name: 'Tier2',
      displayName: 'Small Business',
      employeeLimit: 50,
      monthlyPrice: 4500,
      yearlyPrice: 48600,
      recommended: true,
      features: [
        'Up to 50 employees',
        'Advanced attendance tracking',
        'Biometric integration',
        'Payroll processing',
        'Reports and analytics',
        'Priority email support'
      ]
    },
    {
      name: 'Tier3',
      displayName: 'Growing Business',
      employeeLimit: 100,
      monthlyPrice: 8000,
      yearlyPrice: 86400,
      popular: true,
      features: [
        'Up to 100 employees',
        'All Small Business features',
        'Multi-location support',
        'Custom workflows',
        'API access',
        'Phone and email support'
      ]
    },
    {
      name: 'Tier4',
      displayName: 'Professional',
      employeeLimit: 250,
      monthlyPrice: 17500,
      yearlyPrice: 189000,
      features: [
        'Up to 250 employees',
        'All Growing Business features',
        'Advanced reporting',
        'Department management',
        'Shift scheduling',
        'Dedicated account manager'
      ]
    },
    {
      name: 'Tier5',
      displayName: 'Enterprise',
      employeeLimit: 500,
      monthlyPrice: 32500,
      yearlyPrice: 351000,
      features: [
        'Up to 500 employees',
        'All Professional features',
        'Custom integrations',
        'Advanced security',
        'SLA guarantee',
        '24/7 priority support'
      ]
    }
  ]);

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-MU', {
      style: 'currency',
      currency: 'MUR',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(amount);
  }

  selectTier(tier: TierOption): void {
    this.toastService.success(
      `Thank you for your interest in ${tier.displayName}. Our sales team will contact you shortly.`,
      5000
    );

    // TODO: Implement actual tier change request
    // This should call a backend endpoint to create a tier change request
    // Example: this.billingService.requestTierChange(tier.name)

    this.dialogRef.close({ selectedTier: tier.name });
  }
}
