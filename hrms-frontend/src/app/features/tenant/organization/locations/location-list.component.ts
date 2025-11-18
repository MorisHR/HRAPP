import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { UiModule } from '../../../../shared/ui/ui.module';
import { TableComponent, TableColumn, TableColumnDirective, TooltipDirective } from '../../../../shared/ui';
import { LocationService, LocationDto } from './location.service';

@Component({
  selector: 'app-location-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    TableComponent,
    TableColumnDirective,
    MatButtonModule,
    MatIconModule,
    TooltipDirective,
    UiModule
  ],
  templateUrl: './location-list.component.html',
  styleUrls: ['./location-list.component.scss']
})
export class LocationListComponent implements OnInit {
  locations: LocationDto[] = [];
  loading = false;
  error: string | null = null;

  columns: TableColumn[] = [
    { key: 'locationCode', label: 'Location Code', sortable: true },
    { key: 'locationName', label: 'Name', sortable: true },
    { key: 'type', label: 'Type' },
    { key: 'city', label: 'City', sortable: true },
    { key: 'isActive', label: 'Status' },
    { key: 'actions', label: 'Actions' }
  ];

  constructor(
    private locationService: LocationService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadLocations();
  }

  loadLocations(): void {
    console.log('🔵 [LOCATION-LIST] loadLocations() called');
    this.loading = true;
    this.error = null;

    console.log('🔵 [LOCATION-LIST] Calling locationService.getAll()...');
    this.locationService.getAll().pipe(
      catchError((error) => {
        console.error('❌ [LOCATION-LIST] Error loading locations:', error);

        // Handle specific error types with user-friendly messages
        if (error.status === 401) {
          this.error = 'You are not authenticated. Please log in again.';
        } else if (error.status === 403) {
          this.error = 'You do not have permission to view locations. Please contact your administrator.';
        } else if (error.status === 404) {
          this.error = 'Location service not found. Please contact support.';
        } else if (error.status === 0) {
          this.error = 'Cannot connect to server. Please check your connection or try again later.';
        } else {
          this.error = error?.error?.message || error?.message || 'Failed to load locations. Please try again.';
        }

        return of([]);
      }),
      finalize(() => {
        console.log('🏁 [LOCATION-LIST] finalize() called - setting loading to false');
        this.loading = false;
        this.cdr.detectChanges(); // Force Angular to update the view
      })
    ).subscribe({
      next: (locations) => {
        console.log('✅ [LOCATION-LIST] Subscribe next() called with data:', locations);
        this.locations = locations;
        this.cdr.detectChanges(); // Force Angular to update the view
      },
      error: (err) => {
        console.error('❌ [LOCATION-LIST] Subscribe error() called:', err);
        this.cdr.detectChanges(); // Force Angular to update the view
      },
      complete: () => {
        console.log('🏁 [LOCATION-LIST] Subscribe complete() called');
      }
    });
  }

  onAdd(): void {
    // Navigate to location create form
    this.router.navigate(['/tenant/organization/locations/new']);
  }

  onEdit(id: number): void {
    // Navigate to location edit form
    this.router.navigate(['/tenant/organization/locations', id, 'edit']);
  }

  onDelete(id: number): void {
    // TODO: Implement delete confirmation and deletion
    console.log('Delete location:', id);
  }

  onView(id: number): void {
    // Navigate to location detail view
    this.router.navigate(['/tenant/organization/locations', id]);
  }
}
