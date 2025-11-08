import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { LocationService, LocationDto } from './location.service';

@Component({
  selector: 'app-location-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './location-list.component.html',
  styleUrls: ['./location-list.component.scss']
})
export class LocationListComponent implements OnInit {
  locations$!: Observable<LocationDto[]>;
  loading = false;
  error: string | null = null;
  displayedColumns: string[] = ['code', 'name', 'type', 'city', 'isActive', 'actions'];

  constructor(
    private locationService: LocationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadLocations();
  }

  private loadLocations(): void {
    this.loading = true;
    this.error = null;
    this.locations$ = this.locationService.getAll();
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
