# HRMS Frontend - Fortune 500-Grade Angular Application

> **Modern Human Resource Management System**
> Built with Angular 20, featuring 29 custom UI components, multi-tenant architecture, and enterprise-grade security.

[![Angular](https://img.shields.io/badge/Angular-20.3-red.svg)](https://angular.dev)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.9-blue.svg)](https://www.typescriptlang.org/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Quick Start](#quick-start)
- [Documentation](#documentation)
- [Project Structure](#project-structure)
- [Technology Stack](#technology-stack)
- [Development](#development)
- [Testing](#testing)
- [Deployment](#deployment)
- [Contributing](#contributing)
- [Support](#support)

---

## Overview

HRMS Frontend is a production-ready, enterprise-scale Angular application designed for Fortune 500 companies. It provides comprehensive human resource management capabilities with a focus on:

- **Multi-Tenancy**: Subdomain-based tenant isolation
- **Custom UI Library**: 29 production-ready components (no Material dependency)
- **Security**: JWT authentication, MFA, role-based access control
- **Performance**: Lazy loading, signal-based reactivity, optimized bundles
- **Scalability**: Designed to handle thousands of users per tenant

### Application Portals

1. **SuperAdmin Portal** (`/admin`)
   - Tenant management (CRUD operations)
   - System monitoring and health metrics
   - Audit logs and security alerts
   - Subscription management

2. **Tenant Portal** (`/tenant`)
   - Employee management
   - Attendance tracking with biometric device integration
   - Leave management
   - Payroll processing
   - Comprehensive reporting

3. **Employee Portal** (`/employee`)
   - Personal dashboard
   - Attendance self-service
   - Leave requests
   - Payslip access
   - Profile management

---

## Key Features

### Angular 20 Modern Features

- **Standalone Components**: No NgModules, simplified architecture
- **Signal-Based State**: Reactive state management with Angular Signals
- **Control Flow Syntax**: Modern `@if`, `@for`, `@switch` syntax
- **Lazy Loading**: Route-based code splitting for optimal performance
- **Service Worker**: PWA capabilities with offline support

### Custom UI Component Library

**29 Production-Ready Components:**

- Form Controls: Input, Select, Checkbox, Radio, Toggle, Datepicker
- Data Display: Table, Card, Badge, Chip, List
- Navigation: Tabs, Stepper, Menu, Sidenav, Toolbar
- Feedback: Progress Bar, Spinner, Toast, Dialog
- Layout: Divider, Expansion Panel
- And more...

All components are:
- WCAG 2.1 Level AA compliant
- Keyboard navigable
- Theme-able with CSS variables
- Lightweight (no external UI library dependencies)

### Security Features

- JWT-based authentication with automatic token refresh
- Multi-factor authentication (MFA) for SuperAdmin
- Role-based access control (RBAC)
- Subdomain-based tenant isolation
- HttpOnly cookies for refresh tokens
- CSRF protection
- XSS prevention with Angular sanitization

### Real-Time Capabilities

- SignalR integration for live attendance updates
- WebSocket support for notifications
- Real-time dashboard metrics

### Performance Optimizations

- Lazy-loaded routes (code splitting)
- OnPush change detection strategy
- Signal-based reactivity (no Zone.js overhead)
- Bundle size optimization (< 2MB initial)
- Service worker caching
- Image optimization

---

## Quick Start

### Prerequisites

- **Node.js**: v18.x or v20.x (LTS)
- **npm**: v9.x or higher
- **Angular CLI**: v20.x (optional)

### Installation

```bash
# Clone the repository
git clone <repository-url>
cd hrms-frontend

# Install dependencies
npm install

# Start development server
npm start

# Open browser at http://localhost:4200
```

### First-Time Setup

1. **Configure API endpoint** in `src/environments/environment.ts`:
   ```typescript
   export const environment = {
     production: false,
     apiUrl: 'http://localhost:5090/api'
   };
   ```

2. **Start the application**:
   ```bash
   npm start
   ```

3. **Login as SuperAdmin**:
   - Navigate to: `/auth/superadmin`
   - Use SuperAdmin credentials from backend setup

4. **Create a tenant**:
   - Go to: `/admin/tenants`
   - Click "Add Tenant"
   - Fill in tenant details

5. **Login as Tenant Admin**:
   - Navigate to: `/auth/subdomain`
   - Enter tenant subdomain
   - Login with tenant admin credentials

---

## Documentation

Comprehensive documentation is available in the following guides:

| Document | Description |
|----------|-------------|
| [COMPONENT_LIBRARY_DOCS.md](./COMPONENT_LIBRARY_DOCS.md) | Complete documentation for all 29 custom UI components with API reference and usage examples |
| [DEVELOPER_GUIDE.md](./DEVELOPER_GUIDE.md) | Developer onboarding, coding standards, Git workflow, testing guidelines, and code review checklist |
| [ARCHITECTURE_GUIDE.md](./ARCHITECTURE_GUIDE.md) | Application architecture, module structure, state management, routing, and design patterns |
| [API_INTEGRATION_GUIDE.md](./API_INTEGRATION_GUIDE.md) | Backend API integration, authentication setup, endpoint documentation, and error handling |
| [TROUBLESHOOTING_GUIDE.md](./TROUBLESHOOTING_GUIDE.md) | Common issues, debugging strategies, and solutions |

### Quick Links

- **Component Examples**: `/src/app/shared/ui/components/*/README.md`
- **API Documentation**: See [API_INTEGRATION_GUIDE.md](./API_INTEGRATION_GUIDE.md)
- **Troubleshooting**: See [TROUBLESHOOTING_GUIDE.md](./TROUBLESHOOTING_GUIDE.md)

---

## Project Structure

```
hrms-frontend/
├── src/
│   ├── app/
│   │   ├── core/                          # Singleton services, guards, interceptors
│   │   │   ├── guards/                    # Route guards (auth, role, subdomain)
│   │   │   ├── interceptors/              # HTTP interceptors (auth, tenant, error)
│   │   │   ├── models/                    # TypeScript interfaces and types
│   │   │   └── services/                  # Core business services
│   │   │
│   │   ├── features/                      # Feature modules (lazy-loaded)
│   │   │   ├── admin/                     # SuperAdmin features
│   │   │   │   ├── dashboard/
│   │   │   │   ├── tenant-management/
│   │   │   │   ├── monitoring/
│   │   │   │   └── audit-logs/
│   │   │   ├── tenant/                    # Tenant Admin features
│   │   │   │   ├── dashboard/
│   │   │   │   ├── employees/
│   │   │   │   ├── attendance/
│   │   │   │   ├── leave/
│   │   │   │   ├── payroll/
│   │   │   │   └── reports/
│   │   │   ├── employee/                  # Employee self-service
│   │   │   │   ├── dashboard/
│   │   │   │   ├── attendance/
│   │   │   │   ├── leave/
│   │   │   │   └── payslips/
│   │   │   ├── auth/                      # Authentication pages
│   │   │   └── marketing/                 # Public landing pages
│   │   │
│   │   ├── shared/                        # Shared functionality
│   │   │   ├── ui/components/             # 29 custom UI components
│   │   │   ├── layouts/                   # Layout wrappers
│   │   │   ├── pipes/                     # Custom pipes
│   │   │   └── directives/                # Custom directives
│   │   │
│   │   ├── app.config.ts                  # Application configuration
│   │   └── app.routes.ts                  # Route configuration
│   │
│   ├── environments/                       # Environment-specific configs
│   ├── styles.scss                         # Global styles
│   └── main.ts                             # Application entry point
│
├── public/                                 # Static assets
├── angular.json                            # Angular CLI configuration
├── tsconfig.json                           # TypeScript configuration
├── package.json                            # npm dependencies
└── README.md                               # This file
```

---

## Technology Stack

### Core

- **Angular**: 20.3.x - Latest stable version
- **TypeScript**: 5.9.x - Strict mode enabled
- **RxJS**: 7.8.x - Reactive programming
- **Signals**: Angular's signal-based reactivity

### UI & Styling

- **Custom Components**: 29 production-ready components
- **SCSS**: Styling with BEM methodology
- **Material Icons**: Icon library
- **Chart.js**: Data visualization
- **ng2-charts**: Angular wrapper for Chart.js

### Real-Time

- **SignalR**: Real-time communication with backend
- **WebSocket**: Live updates for attendance, notifications

### Development

- **Angular CLI**: 20.3.x
- **esbuild**: Fast build tool
- **Karma + Jasmine**: Testing framework
- **Prettier**: Code formatting

### DevOps

- **Service Worker**: PWA support with offline capabilities
- **Docker**: Containerization
- **GitHub Actions**: CI/CD pipelines

---

## Development

### Available Scripts

```bash
# Development
npm start                 # Start dev server (http://localhost:4200)
npm run watch            # Build with watch mode

# Building
npm run build            # Production build
npm run build:dev        # Development build

# Testing
npm test                 # Run unit tests
npm run test:coverage    # Run tests with coverage

# Code Quality
npm run lint             # Lint TypeScript code
npm run format           # Format code with Prettier
npm run format:check     # Check code formatting
```

### Development Server

```bash
# Standard development server
npm start

# Custom port
ng serve --port 4300

# With SSL (for testing HTTPS)
ng serve --ssl

# With proxy configuration
ng serve --proxy-config proxy.conf.json
```

### Building

```bash
# Production build (optimized)
npm run build

# Development build (faster, not optimized)
ng build --configuration development

# Analyze bundle size
npm run build -- --stats-json
npx webpack-bundle-analyzer dist/hrms-frontend/browser/stats.json
```

### Code Quality

```bash
# Run linter
ng lint

# Auto-fix linting issues
ng lint --fix

# Format code
npm run format

# Check formatting
npm run format:check
```

---

## Testing

### Unit Tests

```bash
# Run all tests
npm test

# Run tests in headless mode (CI)
npm test -- --browsers=ChromeHeadless --watch=false

# Run tests with coverage
npm test -- --code-coverage

# Run specific test file
npm test -- --include='**/employee.service.spec.ts'
```

### Testing Guidelines

- **Component Tests**: Test component logic and user interactions
- **Service Tests**: Test API calls with HttpClientTestingModule
- **Guard Tests**: Test route protection logic
- **Interceptor Tests**: Test HTTP request/response transformation

Example test:

```typescript
describe('EmployeeService', () => {
  let service: EmployeeService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [EmployeeService]
    });

    service = TestBed.inject(EmployeeService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should fetch employees', () => {
    const mockEmployees = [{ id: 1, name: 'John Doe' }];

    service.getEmployees().subscribe(employees => {
      expect(employees).toEqual(mockEmployees);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/employees`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockEmployees });
  });
});
```

---

## Deployment

### Production Build

```bash
# Create production build
npm run build

# Output directory: dist/hrms-frontend/browser/
```

### Environment Configuration

Update `src/environments/environment.ts` for production:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.hrms.example.com/api',
  appName: 'HRMS',
  version: '1.0.0'
};
```

### Docker Deployment

```dockerfile
# Dockerfile
FROM node:20-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=builder /app/dist/hrms-frontend/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

```bash
# Build Docker image
docker build -t hrms-frontend:latest .

# Run container
docker run -p 80:80 hrms-frontend:latest
```

### Server Configuration

#### Nginx

```nginx
server {
    listen 80;
    server_name hrms.example.com;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # API proxy
    location /api {
        proxy_pass http://backend:5090;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }

    # Gzip compression
    gzip on;
    gzip_types text/plain text/css application/json application/javascript;
}
```

---

## Contributing

### Git Workflow

1. **Create feature branch**:
   ```bash
   git checkout -b feature/add-employee-export
   ```

2. **Make changes and commit**:
   ```bash
   git add .
   git commit -m "feat(employee): Add Excel export functionality"
   ```

3. **Push and create PR**:
   ```bash
   git push origin feature/add-employee-export
   ```

### Commit Message Format

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

<body>

<footer>
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`

Example:
```
feat(employee): Add employee export to Excel

Implemented Excel export using XLSX library. Includes:
- Column selection
- Data filtering
- Custom formatting

Closes #123
```

### Code Review Checklist

- [ ] Code compiles without errors
- [ ] All tests pass
- [ ] No console errors or warnings
- [ ] Code follows style guidelines
- [ ] Documentation updated
- [ ] Accessibility requirements met
- [ ] Responsive design works

---

## Support

### Getting Help

1. **Documentation**: Check the comprehensive guides in this repository
2. **Issues**: Search existing issues or create a new one
3. **Team Chat**: Contact the development team
4. **Stack Overflow**: Search for Angular-specific questions

### Useful Resources

- [Angular Documentation](https://angular.dev)
- [Angular CLI Documentation](https://angular.dev/cli)
- [RxJS Documentation](https://rxjs.dev)
- [TypeScript Documentation](https://www.typescriptlang.org/docs/)

### Reporting Issues

When reporting issues, please include:

- Angular version (`ng version`)
- Node.js version (`node --version`)
- Browser and version
- Steps to reproduce
- Expected vs actual behavior
- Screenshots (if applicable)
- Console errors

---

## License

MIT License - see [LICENSE](LICENSE) file for details

---

## Project Status

- **Version**: 1.0.0
- **Status**: Production-ready
- **Last Updated**: November 2025
- **Maintained By**: HRMS Development Team

---

## Acknowledgments

- Angular Team for the amazing framework
- Open source community for inspiration and tools
- All contributors to this project

---

**Built with care for Fortune 500 enterprises**
