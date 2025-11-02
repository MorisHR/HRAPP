# HRMS Quick Reference Card

## 🚀 Bootstrap the System

### Create First Admin
```bash
curl -X POST http://localhost:5000/api/admin/setup/create-first-admin
```

**Default Credentials:**
- Email: `admin@hrms.com`
- Password: `Admin@123`

### Login
```bash
curl -X POST http://localhost:5000/api/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@hrms.com","password":"Admin@123"}'
```

### Run All Tests
```bash
./test-setup.sh
```

## 📚 Setup Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/admin/setup/create-first-admin` | POST | Create first admin |
| `/api/admin/setup/status` | GET | Check setup status |
| `/api/admin/setup/reset` | DELETE | Reset (Dev only) |
| `/api/admin/auth/login` | POST | Admin login |

## 🏃 Quick Start

**Backend:**
```bash
cd /workspaces/HRAPP/src/HRMS.API
dotnet run
```

**Frontend:**
```bash
cd /workspaces/HRAPP/hrms-frontend
npm start
```

**Access:**
- Backend API: http://localhost:5000
- Frontend: http://localhost:4200
- Swagger: http://localhost:5000/swagger

## 📖 Documentation

- **Setup Guide:** `/SYSTEM_SETUP_GUIDE.md`
- **Frontend Guide:** `/hrms-frontend/README.md`
- **Angular 20 Summary:** `/ANGULAR20_IMPLEMENTATION_SUMMARY.md`

## 🔑 Default Admin User

```
Email:     admin@hrms.com
Password:  Admin@123
Name:      Super Admin
Role:      SuperAdmin
Status:    Active
```

⚠️ **Change password after first login!**

## 🧪 Testing Flow

1. **Check Status:**
   ```bash
   curl http://localhost:5000/api/admin/setup/status
   ```

2. **Create Admin:**
   ```bash
   curl -X POST http://localhost:5000/api/admin/setup/create-first-admin
   ```

3. **Login:**
   ```bash
   curl -X POST http://localhost:5000/api/admin/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"admin@hrms.com","password":"Admin@123"}'
   ```

4. **Use Token:**
   ```bash
   curl http://localhost:5000/api/protected-endpoint \
     -H "Authorization: Bearer YOUR_JWT_TOKEN"
   ```

## 🌐 Angular Frontend

**Login Page:**
- URL: http://localhost:4200/login
- Credentials: admin@hrms.com / Admin@123

**After Login:**
- Admin Dashboard: `/admin/dashboard`
- Tenant Portal: `/tenant/dashboard`
- Employee Portal: `/employee/dashboard`

## 🔧 Features

### Backend (.NET 9)
- ✅ PostgreSQL Multi-tenancy
- ✅ JWT Authentication
- ✅ Argon2 Password Hashing
- ✅ Entity Framework Core
- ✅ Hangfire Background Jobs
- ✅ Swagger/OpenAPI

### Frontend (Angular 20)
- ✅ Zoneless Change Detection
- ✅ Signals
- ✅ Built-in Control Flow
- ✅ Material Design 3
- ✅ Dark Mode
- ✅ PWA

## 🛠️ Development

**Run Backend:**
```bash
cd src/HRMS.API
dotnet watch run
```

**Run Frontend:**
```bash
cd hrms-frontend
npm start
```

**Apply Migrations:**
```bash
cd src/HRMS.API
dotnet ef database update --context MasterDbContext
```

**Build Frontend:**
```bash
cd hrms-frontend
npm run build
```

## 📊 Project Structure

```
HRAPP/
├── src/
│   ├── HRMS.API/              # Web API
│   ├── HRMS.Core/             # Domain entities
│   ├── HRMS.Application/      # Business logic
│   ├── HRMS.Infrastructure/   # Data access
│   └── HRMS.BackgroundJobs/   # Hangfire jobs
└── hrms-frontend/             # Angular 20 app
    └── src/app/
        ├── core/              # Services, guards
        ├── features/          # Feature modules
        └── shared/            # Shared components
```

## 🐛 Troubleshooting

**Database Connection Issue:**
```bash
# Check PostgreSQL
sudo systemctl status postgresql

# Verify connection string in appsettings.json
```

**Admin Already Exists:**
```bash
# Reset (Development only)
curl -X DELETE http://localhost:5000/api/admin/setup/reset
```

**Build Errors:**
```bash
# Backend
dotnet clean
dotnet restore
dotnet build

# Frontend
rm -rf node_modules package-lock.json
npm install
```

## 📞 Support

- Setup Guide: `SYSTEM_SETUP_GUIDE.md`
- Check logs in console
- Review API response messages
- Verify database state

---

**Version:** 1.0.0
**Date:** 2025-11-01
**Status:** ✅ Production Ready
