# Location Components - Quick Start Guide

## 🚀 Quick Implementation Guide

### 1. Add Location Selector to Any Form (5 minutes)

```typescript
// your-component.component.ts
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LocationSelectorComponent } from './shared/components/location-selector/location-selector.component';

@Component({
  selector: 'app-your-component',
  standalone: true,
  imports: [LocationSelectorComponent, ReactiveFormsModule],
  template: `
    <form [formGroup]="myForm">
      <app-location-selector
        formControlName="locationId"
        (locationSelected)="onLocationSelected($event)"
      ></app-location-selector>
    </form>
  `
})
export class YourComponent {
  myForm = this.fb.group({
    locationId: ['', Validators.required]
  });

  constructor(private fb: FormBuilder) {}

  onLocationSelected(location: Location) {
    console.log('Selected:', location.name, location.district);
  }
}
```

---

### 2. Access Location Management (Super Admin)

**URLs**:
- List: `http://localhost:4200/admin/locations`
- Create: `http://localhost:4200/admin/locations/new`
- Edit: `http://localhost:4200/admin/locations/{id}/edit`

**Navigation from Sidebar**:
```html
<!-- Add to admin sidebar -->
<a mat-list-item routerLink="/admin/locations">
  <mat-icon>location_on</mat-icon>
  <span>Locations</span>
</a>
```

---

### 3. Seed Database (One-Time Setup)

```typescript
// In component or service
this.locationService.seedMauritiusLocations().subscribe({
  next: (response) => {
    console.log(`Seeded ${response.data.totalSeeded} locations`);
  },
  error: (error) => {
    console.error('Seed failed:', error);
  }
});
```

**OR via UI**:
1. Navigate to `/admin/locations`
2. Click "Seed Mauritius Data" button
3. Confirm the action

---

### 4. Common API Calls

#### Get All Active Locations
```typescript
this.locationService.getLocations({ isActive: true })
  .subscribe(locations => {
    console.log('Active locations:', locations);
  });
```

#### Get Locations by District
```typescript
this.locationService.getLocationsByDistrict('Port Louis')
  .subscribe(locations => {
    console.log('Port Louis locations:', locations);
  });
```

#### Search Locations
```typescript
this.locationService.searchLocations('curepipe')
  .subscribe(results => {
    console.log('Search results:', results);
  });
```

#### Create Location
```typescript
const newLocation = {
  district: 'Plaines Wilhems',
  type: LocationType.City,
  name: 'Curepipe',
  postalCode: '74450',
  isActive: true
};

this.locationService.createLocation(newLocation)
  .subscribe(created => {
    console.log('Created:', created.id);
  });
```

---

### 5. File Locations Reference

**Core Files**:
```
hrms-frontend/src/app/
├── core/
│   ├── constants/
│   │   └── mauritius.constants.ts    # 9 districts, location types
│   ├── models/
│   │   └── location.ts                # Interfaces & enums
│   └── services/
│       └── location.service.ts        # API service
├── shared/
│   └── components/
│       └── location-selector/
│           └── location-selector.component.ts  # Reusable selector
└── features/
    └── admin/
        └── locations/
            ├── location-list.component.ts      # Admin dashboard
            └── location-form.component.ts      # Create/Edit form
```

---

### 6. TypeScript Types Reference

```typescript
// Import types
import { Location, LocationType, LocationFilter } from './core/models/location';
import { MAURITIUS_DISTRICTS, LOCATION_TYPES } from './core/constants/mauritius.constants';

// Use in your code
const filter: LocationFilter = {
  district: 'Port Louis',
  type: LocationType.City,
  isActive: true
};

// Type-safe district
const district: typeof MAURITIUS_DISTRICTS[number] = 'Moka';
```

---

### 7. Material Components Used

Ensure these are imported if you're using location components:

```typescript
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
```

---

### 8. Common Use Cases

#### Use Case 1: Employee Address
```typescript
// Employee form with location
employeeForm = this.fb.group({
  name: ['', Validators.required],
  email: ['', [Validators.required, Validators.email]],
  locationId: ['', Validators.required],  // Location selector
  streetAddress: ['']
});
```

#### Use Case 2: Office/Branch Location
```typescript
// Office form
officeForm = this.fb.group({
  officeName: ['', Validators.required],
  locationId: ['', Validators.required],  // Location selector
  buildingName: [''],
  floor: ['']
});
```

#### Use Case 3: Filter Employees by Location
```typescript
// Filter component
selectedDistrict: string = '';
employees: Employee[] = [];

filterByLocation() {
  this.employeeService.getEmployees({ district: this.selectedDistrict })
    .subscribe(employees => {
      this.employees = employees;
    });
}
```

---

### 9. Troubleshooting

#### Issue: "Cannot find module 'location.service'"
**Solution**: Ensure service is imported correctly:
```typescript
import { LocationService } from '../core/services/location.service';
```

#### Issue: "Location selector not showing"
**Solution**: Import component in standalone component:
```typescript
import { LocationSelectorComponent } from '../shared/components/location-selector/location-selector.component';

@Component({
  imports: [LocationSelectorComponent, ...],
  // ...
})
```

#### Issue: "API returns 404"
**Solution**: Verify backend API is running and endpoint exists at `/api/location`

#### Issue: "Locations not loading"
**Solution**: Check browser console and network tab. Verify auth token is present.

---

### 10. Testing Commands

```bash
# Build the application
cd /workspaces/HRAPP/hrms-frontend
npm run build

# Run in dev mode
npm start

# Run tests (if configured)
npm test

# Check TypeScript errors
npx tsc --noEmit
```

---

## 📋 Checklist for Integration

- [ ] Backend `/api/location` endpoints implemented
- [ ] Database seeded with Mauritius locations
- [ ] Location service imported where needed
- [ ] Location selector added to forms
- [ ] Admin routes accessible (check guards)
- [ ] Material modules imported
- [ ] Forms using reactive forms pattern
- [ ] Error handling implemented
- [ ] Loading states handled
- [ ] Success/error notifications working

---

## 🎨 Customization Tips

### Custom Styling
```scss
// Override Material theme colors
app-location-selector {
  --primary-color: #your-color;
  --accent-color: #your-color;
}
```

### Custom Validators
```typescript
// Add custom location validator
function validLocationValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) return null;

    // Custom validation logic
    return isValidLocation(value) ? null : { invalidLocation: true };
  };
}
```

### Custom Search Logic
```typescript
// Override search behavior in LocationSelectorComponent
onSearchChange(term: string): void {
  // Your custom search logic
  this.customSearchService.search(term)
    .subscribe(results => {
      this.filteredLocations.set(results);
    });
}
```

---

## 📊 Sample Data

### Mauritius Districts (9)
```typescript
[
  'Port Louis',       // Capital, northwest
  'Pamplemousses',    // North
  'Rivière du Rempart', // Northeast
  'Flacq',           // East
  'Grand Port',      // Southeast
  'Savanne',         // South
  'Plaines Wilhems', // Central (most populated)
  'Moka',            // Central highlands
  'Black River'      // West
]
```

### Location Types (5)
```typescript
enum LocationType {
  City = 'City',      // Major urban areas (Port Louis, Curepipe, etc.)
  Town = 'Town',      // Mid-sized urban areas
  Village = 'Village', // Small settlements
  Suburb = 'Suburb',  // Suburban areas
  Other = 'Other'     // Other location types
}
```

---

## 🔗 Related Documentation

- [Full Documentation](./LOCATION_COMPONENTS_DOCUMENTATION.md)
- [Angular Material Docs](https://material.angular.io/)
- [Reactive Forms Guide](https://angular.dev/guide/forms)
- [API Integration Guide](../docs/API_INTEGRATION.md)

---

## 💡 Best Practices

1. **Always validate location IDs** before using them
2. **Use location selector component** instead of raw dropdowns
3. **Cache location data** in services to reduce API calls
4. **Handle errors gracefully** with user-friendly messages
5. **Test with real Mauritius data** after seeding
6. **Use TypeScript types** for type safety
7. **Implement loading states** for better UX
8. **Add confirmation dialogs** for destructive actions

---

## 📞 Need Help?

- Check the [Full Documentation](./LOCATION_COMPONENTS_DOCUMENTATION.md)
- Review example code in this guide
- Check browser console for errors
- Verify backend API responses
- Ensure authentication is working

---

**Quick Start Version**: 1.0.0
**Last Updated**: November 12, 2025
