import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PerformanceMonitoringService } from './core/services/performance-monitoring.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected readonly title = signal('hrms-frontend');
  private performanceMonitoring = inject(PerformanceMonitoringService);

  ngOnInit(): void {
    // Initialize Real User Monitoring (RUM) for frontend performance tracking
    this.performanceMonitoring.initialize();
  }
}
