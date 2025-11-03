# HRMS Frontend - Quick Start Guide

## 🚀 Get Started in 3 Minutes

### Prerequisites
- Node.js v20+ installed ✅ (Currently: v22.17.0)
- npm installed ✅ (Currently: v9.8.1)

### Installation

```bash
cd hrms-frontend
npm install
npm start
```

Open your browser at **http://localhost:4200**

## 🎯 Test the Application

### Login Credentials (Mock)
Since the backend isn't running yet, you'll need to implement mock data or connect to your .NET backend API at `http://localhost:5000/api`.

**Expected API Endpoints:**
- POST `/api/auth/admin/login` - Admin login
- POST `/api/auth/tenant/login` - Tenant login
- GET `/api/tenants` - List tenants
- GET `/api/employees` - List employees

### Navigate the App

1. **Login Page** → `/login`
2. **Admin Dashboard** → `/admin/dashboard` (SuperAdmin only)
3. **Tenant Portal** → `/tenant/dashboard` (HR/Admin)
4. **Employee Portal** → `/employee/dashboard` (All employees)

## 🎨 Toggle Dark Mode

Click the theme toggle button (☀️/🌙) in the toolbar to switch between light and dark modes!

## 📱 Install as PWA

1. Build for production: `npm run build`
2. Serve: `npx http-server dist/hrms-frontend -p 8080`
3. Open in Chrome and click "Install App" prompt

## 🔧 Key Commands

```bash
# Development server
npm start

# Production build
npm run build

# Run tests
npm test

# Watch mode
npm run watch
```

## 🏗️ Project Structure

```
src/app/
├── core/               # Services, Guards, Interceptors, Models
├── features/
│   ├── admin/         # Admin Portal
│   ├── tenant/        # Tenant Portal
│   └── employee/      # Employee Portal
├── shared/            # Shared components
└── app.routes.ts      # Routing config
```

## 🎓 Angular 20 Features Used

- ✅ Zoneless Change Detection
- ✅ Signals (`signal()`, `computed()`, `effect()`)
- ✅ Built-in Control Flow (`@if`, `@for`)
- ✅ Standalone Components
- ✅ Material Design 3
- ✅ PWA Support

## 🔗 Connect to Backend

Update `src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api', // Your .NET API
  appName: 'HRMS',
  version: '1.0.0'
};
```

## 📚 Next Steps

1. **Connect Backend**: Ensure .NET API is running at port 5000
2. **Implement Mock Data**: Or use json-server for testing
3. **Add Features**: Employee list, attendance module, leave module
4. **Customize Theme**: Edit `src/styles.scss`
5. **Deploy**: Build and deploy to your hosting service

## 🐛 Troubleshooting

**Port 4200 already in use?**
```bash
ng serve --port 4300
```

**Build errors?**
```bash
rm -rf node_modules package-lock.json
npm install
```

**API connection errors?**
Check that your backend is running and CORS is configured properly.

## 📖 Documentation

- Full documentation: `README.md`
- Implementation details: `/ANGULAR20_IMPLEMENTATION_SUMMARY.md`
- Angular 20 docs: https://angular.dev

## 🎉 You're Ready!

The HRMS frontend is now running with Angular 20's latest features. Happy coding!
