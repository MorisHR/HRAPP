import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SecurityAlertsDashboardComponent } from './components/dashboard/security-alerts-dashboard.component';
import { AlertListComponent } from './components/list/alert-list.component';
import { AlertDetailComponent } from './components/detail/alert-detail.component';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    component: SecurityAlertsDashboardComponent,
    data: { title: 'Security Alerts Dashboard' }
  },
  {
    path: 'list',
    component: AlertListComponent,
    data: { title: 'Security Alerts List' }
  },
  {
    path: 'detail/:id',
    component: AlertDetailComponent,
    data: { title: 'Alert Details' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SecurityAlertsRoutingModule { }
