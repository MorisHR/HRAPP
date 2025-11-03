# HRMS Frontend - Angular 20

Modern HRMS (Human Resource Management System) frontend built with **Angular 20**, featuring the latest stable features including signals, zoneless change detection, and Material Design 3.

## Features

### Angular 20 Features
- **Zoneless Change Detection** - Faster performance without Zone.js
- **Signals** - Reactive state management with Angular Signals
- **Standalone Components** - No NgModules required
- **Built-in Control Flow** - @if, @for, @switch syntax
- **effect()** - Stable side effects with signals
- **Material Design 3** - Latest Material components
- **PWA Support** - Progressive Web App capabilities

### Application Features

#### Admin Portal (admin.hrms.com)
- Login with JWT authentication
- Dashboard with tenant analytics
- Tenant Management:
  - List tenants with Material table
  - Create tenant wizard (30+ industry sectors)
  - View/Edit/Suspend/Delete tenants
  - Usage statistics

#### Tenant Portal (tenant.hrms.com)
- HR Dashboard with KPIs
- Employee Module (list, add, profile)
- Attendance Module (daily board, overtime)
- Leave Module (apply, approve, calendar)
- Payroll Module (cycles, payslips, reports)
- Reports (filters, charts, export)

#### Employee Self-Service Portal
- Dashboard
- Mark attendance
- Apply for leave
- View payslips
- Profile management

### Modern UI/UX
- Material Design 3 theme
- Dark mode support
- Responsive design (mobile-first)
- Smooth animations
- PWA installable

## Prerequisites

- Node.js v20+ (Currently using v22.17.0)
- npm 9.8.1+
- Angular CLI 20.3.8

## Installation

```bash
# Install dependencies
npm install

# Start development server
npm start

# Open browser at http://localhost:4200
```

## Available Scripts

- `npm start` - Start development server
- `npm run build` - Build for production
- `npm run watch` - Build with watch mode
- `npm test` - Run unit tests

## Project Structure

```
src/
├── app/
│   ├── core/                     # Core services, guards, interceptors
│   │   ├── guards/               # Auth and role guards
│   │   ├── interceptors/         # HTTP interceptors (JWT)
│   │   ├── models/               # TypeScript interfaces
│   │   └── services/             # Business services
│   ├── features/                 # Feature modules
│   │   ├── admin/               # Admin portal
│   │   ├── tenant/              # Tenant portal
│   │   └── employee/            # Employee portal
│   ├── shared/                  # Shared components
│   ├── app.config.ts            # Application configuration
│   └── app.routes.ts            # Routing configuration
├── environments/                # Environment configs
└── styles.scss                  # Global styles & Material theme
```

## Architecture

### Zoneless Change Detection
The app uses Angular 20's experimental zoneless change detection for better performance:

```typescript
provideExperimentalZonelessChangeDetection()
```

### Signals-based State Management
All services use Angular Signals for reactive state:

```typescript
private userSignal = signal<User | null>(null);
readonly user = this.userSignal.asReadonly();
readonly isAuthenticated = computed(() => !!this.userSignal());
```

### Modern Control Flow
Components use Angular 20's built-in control flow:

```html
@if (loading()) {
  <mat-spinner></mat-spinner>
} @else {
  <div>{{ data() }}</div>
}
```

## API Integration

The frontend expects a REST API at `http://localhost:5000/api`. Update `src/environments/environment.ts` to change the API URL.

## Building for Production

```bash
npm run build
```

The build artifacts will be stored in the `dist/` directory with AOT compilation, tree shaking, and service worker for PWA.

## PWA Configuration

The app is PWA-ready with service worker, manifest, and app icons. Install it on mobile or desktop for a native-like experience.

## Theme Customization

Toggle dark mode using the theme button in the toolbar. Customize themes in `src/styles.scss`.

## License

MIT
