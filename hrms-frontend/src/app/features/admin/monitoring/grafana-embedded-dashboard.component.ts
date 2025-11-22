import { Component } from '@angular/core';
import { EmbeddedGrafanaPanelComponent } from './embedded-grafana-panel.component';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';

/**
 * OPTIONAL: Embedded Grafana Dashboard for SuperAdmin
 *
 * Shows technical monitoring metrics within the Angular app
 * Alternative to accessing Grafana directly at port 3000
 *
 * Use Case: SuperAdmin wants to see infrastructure metrics without leaving the app
 *
 * Route: /admin/monitoring/grafana
 * Role: SuperAdmin only
 */
@Component({
  selector: 'app-grafana-embedded-dashboard',
  standalone: true,
  imports: [
    EmbeddedGrafanaPanelComponent,
    MatTabsModule,
    MatCardModule
  ],
  template: `
    <div class="grafana-dashboard-container">
      <h1>Technical Monitoring (Embedded Grafana)</h1>

      <p class="info-banner">
        💡 <strong>Tip:</strong> For full functionality, access Grafana directly at
        <a href="http://localhost:3000" target="_blank">http://localhost:3000</a>
      </p>

      <mat-tab-group>
        <!-- Frontend RUM Tab -->
        <mat-tab label="Frontend Performance">
          <div class="tab-content">
            <mat-card>
              <mat-card-header>
                <mat-card-title>Core Web Vitals</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <app-embedded-grafana-panel
                  dashboardId="frontend-rum"
                  panelId="1"
                  height="300px"
                  [timeRange]="{from: 'now-1h', to: 'now'}"
                  title="Core Web Vitals Overview">
                </app-embedded-grafana-panel>
              </mat-card-content>
            </mat-card>

            <mat-card>
              <mat-card-header>
                <mat-card-title>Largest Contentful Paint (LCP)</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <app-embedded-grafana-panel
                  dashboardId="frontend-rum"
                  panelId="2"
                  height="400px"
                  [timeRange]="{from: 'now-1h', to: 'now'}"
                  title="LCP by URL">
                </app-embedded-grafana-panel>
              </mat-card-content>
            </mat-card>

            <mat-card>
              <mat-card-header>
                <mat-card-title>Frontend Error Rate</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <app-embedded-grafana-panel
                  dashboardId="frontend-rum"
                  panelId="5"
                  height="400px"
                  [timeRange]="{from: 'now-1h', to: 'now'}"
                  title="Frontend Errors">
                </app-embedded-grafana-panel>
              </mat-card-content>
            </mat-card>
          </div>
        </mat-tab>

        <!-- Backend Performance Tab -->
        <mat-tab label="Backend Performance">
          <div class="tab-content">
            <mat-card>
              <mat-card-header>
                <mat-card-title>API Request Duration</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <app-embedded-grafana-panel
                  dashboardId="hrms-api"
                  panelId="1"
                  height="400px"
                  [timeRange]="{from: 'now-1h', to: 'now'}"
                  title="API Performance">
                </app-embedded-grafana-panel>
              </mat-card-content>
            </mat-card>

            <mat-card>
              <mat-card-header>
                <mat-card-title>Database Performance</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <app-embedded-grafana-panel
                  dashboardId="hrms-api"
                  panelId="2"
                  height="400px"
                  [timeRange]="{from: 'now-1h', to: 'now'}"
                  title="Database Metrics">
                </app-embedded-grafana-panel>
              </mat-card-content>
            </mat-card>
          </div>
        </mat-tab>

        <!-- Infrastructure Tab -->
        <mat-tab label="Infrastructure">
          <div class="tab-content">
            <mat-card>
              <mat-card-header>
                <mat-card-title>System Resources</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <app-embedded-grafana-panel
                  dashboardId="infrastructure"
                  panelId="1"
                  height="400px"
                  [timeRange]="{from: 'now-1h', to: 'now'}"
                  title="CPU, Memory, Disk">
                </app-embedded-grafana-panel>
              </mat-card-content>
            </mat-card>

            <mat-card>
              <mat-card-header>
                <mat-card-title>Database Connections</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <app-embedded-grafana-panel
                  dashboardId="infrastructure"
                  panelId="2"
                  height="400px"
                  [timeRange]="{from: 'now-1h', to: 'now'}"
                  title="Connection Pool">
                </app-embedded-grafana-panel>
              </mat-card-content>
            </mat-card>
          </div>
        </mat-tab>
      </mat-tab-group>

      <div class="actions">
        <a
          href="http://localhost:3000"
          target="_blank"
          mat-raised-button
          color="primary">
          Open Full Grafana Dashboard
        </a>
      </div>
    </div>
  `,
  styles: [`
    .grafana-dashboard-container {
      padding: 24px;
      max-width: 1400px;
      margin: 0 auto;
    }

    h1 {
      margin-bottom: 16px;
      color: #333;
    }

    .info-banner {
      background-color: #e3f2fd;
      padding: 12px 16px;
      border-radius: 4px;
      margin-bottom: 24px;
      border-left: 4px solid #2196f3;
    }

    .info-banner a {
      color: #1976d2;
      text-decoration: none;
      font-weight: 500;
    }

    .info-banner a:hover {
      text-decoration: underline;
    }

    .tab-content {
      padding: 24px 0;
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    mat-card {
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    mat-card-header {
      margin-bottom: 16px;
    }

    .actions {
      margin-top: 32px;
      text-align: center;
    }
  `]
})
export class GrafanaEmbeddedDashboardComponent {}
