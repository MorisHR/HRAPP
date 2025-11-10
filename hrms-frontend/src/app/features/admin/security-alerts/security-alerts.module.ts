import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';

import { SecurityAlertsRoutingModule } from './security-alerts-routing.module';

// Components
import { SecurityAlertsDashboardComponent } from './components/dashboard/security-alerts-dashboard.component';
import { AlertListComponent } from './components/list/alert-list.component';
import { AlertDetailComponent } from './components/detail/alert-detail.component';

// Service
import { SecurityAlertService } from '../../../services/security-alert.service';

@NgModule({
  declarations: [
    SecurityAlertsDashboardComponent,
    AlertListComponent,
    AlertDetailComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    HttpClientModule,
    SecurityAlertsRoutingModule
  ],
  providers: [
    SecurityAlertService
  ]
})
export class SecurityAlertsModule { }
