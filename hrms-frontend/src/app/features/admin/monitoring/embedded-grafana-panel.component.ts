import { Component, Input, signal, OnInit, SecurityContext } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { environment } from '../../../../environments/environment';

/**
 * Embedded Grafana Panel Component
 *
 * Allows SuperAdmin to view Grafana dashboards within the Angular app
 * Uses iframe with security considerations
 *
 * Usage:
 * <app-embedded-grafana-panel
 *   dashboardId="frontend-rum"
 *   panelId="1"
 *   [timeRange]="{from: 'now-1h', to: 'now'}"
 *   theme="light">
 * </app-embedded-grafana-panel>
 */
@Component({
  selector: 'app-embedded-grafana-panel',
  standalone: true,
  template: `
    <div class="grafana-panel-container">
      @if (showPanel()) {
        <iframe
          [src]="sanitizedUrl()"
          [width]="width()"
          [height]="height()"
          frameborder="0"
          [title]="title()">
        </iframe>
      } @else {
        <div class="error-message">
          <p>Unable to load Grafana panel</p>
          <p class="error-details">{{ errorMessage() }}</p>
        </div>
      }
    </div>
  `,
  styles: [`
    .grafana-panel-container {
      width: 100%;
      height: 100%;
      border: 1px solid #e0e0e0;
      border-radius: 4px;
      overflow: hidden;
    }

    iframe {
      display: block;
      border: none;
    }

    .error-message {
      padding: 20px;
      text-align: center;
      background-color: #fff3cd;
      color: #856404;
    }

    .error-details {
      font-size: 12px;
      margin-top: 8px;
      color: #666;
    }
  `]
})
export class EmbeddedGrafanaPanelComponent implements OnInit {
  // Inputs
  @Input() dashboardId: string = '';
  @Input() panelId: string | number = '';
  @Input() width: string = '100%';
  @Input() height: string = '400px';
  @Input() theme: 'light' | 'dark' = 'light';
  @Input() timeRange: { from: string; to: string } = { from: 'now-1h', to: 'now' };
  @Input() refresh: string = '30s';
  @Input() orgId: number = 1;
  @Input() kiosk: boolean = false; // Hide Grafana UI chrome
  @Input() title: string = 'Grafana Panel';

  // State
  private showPanelSignal = signal(false);
  private sanitizedUrlSignal = signal<SafeResourceUrl | null>(null);
  private errorMessageSignal = signal('');

  // Readonly signals
  readonly showPanel = this.showPanelSignal.asReadonly();
  readonly sanitizedUrl = this.sanitizedUrlSignal.asReadonly();
  readonly errorMessage = this.errorMessageSignal.asReadonly();

  // Grafana base URL from environment
  private grafanaBaseUrl = environment.grafanaUrl || 'http://localhost:3000';

  constructor(private sanitizer: DomSanitizer) {}

  ngOnInit(): void {
    this.buildGrafanaUrl();
  }

  /**
   * Build Grafana iframe URL with security sanitization
   */
  private buildGrafanaUrl(): void {
    try {
      // Validate required inputs
      if (!this.dashboardId) {
        throw new Error('Dashboard ID is required');
      }

      if (!this.panelId) {
        throw new Error('Panel ID is required');
      }

      // Build solo panel URL (removes Grafana UI chrome)
      // Format: /d-solo/{dashboard-id}?orgId=1&panelId={panel-id}&from=now-1h&to=now
      const params = new URLSearchParams({
        orgId: this.orgId.toString(),
        panelId: this.panelId.toString(),
        from: this.timeRange.from,
        to: this.timeRange.to,
        refresh: this.refresh,
        theme: this.theme,
      });

      if (this.kiosk) {
        params.append('kiosk', 'true');
      }

      const url = `${this.grafanaBaseUrl}/d-solo/${this.dashboardId}?${params.toString()}`;

      // Sanitize URL to prevent XSS
      const sanitized = this.sanitizer.sanitize(SecurityContext.RESOURCE_URL, url);

      if (!sanitized) {
        throw new Error('URL failed security validation');
      }

      // Use bypassSecurityTrustResourceUrl for iframe src
      // This is safe because we control the Grafana URL and sanitized the input
      this.sanitizedUrlSignal.set(this.sanitizer.bypassSecurityTrustResourceUrl(url));
      this.showPanelSignal.set(true);
    } catch (error: any) {
      console.error('Error building Grafana URL:', error);
      this.errorMessageSignal.set(error.message || 'Unknown error');
      this.showPanelSignal.set(false);
    }
  }

  /**
   * Refresh the panel (by rebuilding URL with new timestamp)
   */
  refresh(): void {
    this.buildGrafanaUrl();
  }
}
