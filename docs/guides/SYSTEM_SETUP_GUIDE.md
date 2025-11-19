# HRMS System Setup Guide

## 🚀 First-Time System Initialization

This guide explains how to bootstrap the HRMS system by creating the first admin user.

## Setup Endpoints

### 1. Create First Admin User

**Endpoint:** `POST /api/admin/setup/create-first-admin`

**Purpose:** Creates the initial admin user for system bootstrap

**Request:**
```bash
curl -X POST http://localhost:5000/api/admin/setup/create-first-admin
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Admin user created successfully. Email: admin@hrms.com, Password: Admin@123",
  "data": {
    "email": "admin@hrms.com",
    "password": "Admin@123",
    "firstName": "Super",
    "lastName": "Admin",
    "isActive": true,
    "warning": "⚠️ Please change this password after first login!"
  }
}
```

**Error Response - Admin Already Exists (409 Conflict):**
```json
{
  "success": false,
  "message": "Admin user already exists. Cannot create duplicate."
}
```

**Error Response - Server Error (500):**
```json
{
  "success": false,
  "message": "An error occurred while creating the admin user",
  "error": "Error details..."
}
```

### 2. Check Setup Status

**Endpoint:** `GET /api/admin/setup/status`

**Purpose:** Check if the system has been initialized

**Request:**
```bash
curl http://localhost:5000/api/admin/setup/status
```

**Response:**
```json
{
  "success": true,
  "data": {
    "isSetupComplete": true,
    "adminUserCount": 1,
    "message": "System is set up. Admin users exist."
  }
}
```

### 3. Reset System (Development Only)

**Endpoint:** `DELETE /api/admin/setup/reset`

**Purpose:** Delete all admin users (only works in Development environment)

**Request:**
```bash
curl -X DELETE http://localhost:5000/api/admin/setup/reset
```

**Response:**
```json
{
  "success": true,
  "message": "System reset successfully. Deleted 1 admin user(s).",
  "deletedCount": 1
}
```

**Note:** This endpoint is **ONLY** available in Development environment!

## Default Admin Credentials

After running the setup endpoint, use these credentials to login:

- **Email:** `admin@hrms.com`
- **Password:** `Admin@123`
- **Role:** Super Admin
- **Name:** Super Admin

## Security Features

### Password Hashing
- Passwords are hashed using **Argon2id** algorithm
- Configuration:
  - Salt Size: 16 bytes (128 bits)
  - Hash Size: 32 bytes (256 bits)
  - Iterations: 4
  - Memory Size: 65536 KB (64 MB)
  - Degree of Parallelism: 1

### Authentication Flow
1. User logs in with email and password
2. System verifies password against Argon2 hash
3. JWT token generated with claims
4. Token expires in configured time (default: 60 minutes)
5. Role claim: "SuperAdmin"

## Step-by-Step Setup Process

### Prerequisites
1. PostgreSQL database running
2. Database migrations applied
3. .NET 8 SDK installed
4. appsettings.json configured

### Setup Steps

**Step 1: Ensure Database is Running**
```bash
# Check PostgreSQL is running
sudo systemctl status postgresql

# Or if using Docker
docker ps | grep postgres
```

**Step 2: Apply Migrations (if not already done)**
```bash
cd /workspaces/HRAPP/src/HRMS.API
dotnet ef database update --context MasterDbContext
```

**Step 3: Start the API**
```bash
cd /workspaces/HRAPP/src/HRMS.API
dotnet run
```

**Step 4: Check Setup Status**
```bash
curl http://localhost:5000/api/admin/setup/status
```

**Step 5: Create First Admin**
```bash
curl -X POST http://localhost:5000/api/admin/setup/create-first-admin
```

**Step 6: Login with Admin Credentials**
```bash
curl -X POST http://localhost:5000/api/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@hrms.com",
    "password": "Admin@123"
  }'
```

**Response:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-11-01T08:16:38.470Z",
    "adminUser": {
      "id": "guid-here",
      "userName": "admin",
      "email": "admin@hrms.com",
      "isActive": true,
      "lastLoginDate": "2025-11-01T07:16:38.470Z",
      "isTwoFactorEnabled": false,
      "createdAt": "2025-11-01T07:16:38.470Z"
    }
  },
  "message": "Login successful"
}
```

## Testing with Angular Frontend

After creating the admin user, you can test the full stack:

**1. Start the Backend API:**
```bash
cd /workspaces/HRAPP/src/HRMS.API
dotnet run
```

**2. Start the Angular Frontend:**
```bash
cd /workspaces/HRAPP/hrms-frontend
npm start
```

**3. Access the Application:**
- Open browser: `http://localhost:4200`
- You'll be redirected to `/login`
- Enter credentials:
  - Email: `admin@hrms.com`
  - Password: `Admin@123`
- After successful login, you'll be redirected to Admin Dashboard

## Database Table Structure

The admin user is stored in the `master.admin_users` table:

```sql
-- View the created admin user
SELECT
    id,
    user_name,
    email,
    first_name,
    last_name,
    is_active,
    created_at,
    last_login_date
FROM master.admin_users;
```

## Troubleshooting

### Issue: "Admin user already exists"
**Solution:**
- If you need to recreate the admin, use the reset endpoint (Development only):
  ```bash
  curl -X DELETE http://localhost:5000/api/admin/setup/reset
  ```
- Then run the create-first-admin endpoint again

### Issue: "Connection refused" or "Cannot connect to database"
**Solution:**
- Ensure PostgreSQL is running
- Check connection string in `appsettings.json`
- Verify database exists and migrations are applied

### Issue: "Login failed - Invalid email or password"
**Solution:**
- Ensure you're using the correct credentials:
  - Email: `admin@hrms.com`
  - Password: `Admin@123`
- Check that the admin user was created successfully
- Verify the password hash in the database is not null

### Issue: "401 Unauthorized" when accessing protected endpoints
**Solution:**
- Ensure you're including the JWT token in the Authorization header:
  ```bash
  curl http://localhost:5000/api/protected-endpoint \
    -H "Authorization: Bearer YOUR_JWT_TOKEN"
  ```

## Security Best Practices

### Production Deployment
Before deploying to production:

1. **Change Default Password:**
   - Login with default credentials
   - Implement password change endpoint
   - Force password change on first login

2. **Disable Reset Endpoint:**
   - The reset endpoint is already restricted to Development environment
   - Do NOT set `ASPNETCORE_ENVIRONMENT=Development` in production

3. **Use Strong Passwords:**
   - Minimum 8 characters
   - Include uppercase, lowercase, numbers, and special characters
   - Consider implementing password complexity requirements

4. **Enable Two-Factor Authentication:**
   - Implement 2FA for admin accounts
   - Use TOTP (Time-based One-Time Password)

5. **Rotate JWT Secret:**
   - Use a strong, random JWT secret
   - Store in environment variables or Azure Key Vault
   - Rotate periodically

6. **Monitor Admin Access:**
   - Log all admin login attempts
   - Track admin activities
   - Set up alerts for suspicious activities

## API Endpoints Summary

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/admin/setup/create-first-admin` | Create first admin | No |
| GET | `/api/admin/setup/status` | Check setup status | No |
| DELETE | `/api/admin/setup/reset` | Reset system (Dev only) | No |
| POST | `/api/admin/auth/login` | Admin login | No |
| GET | `/api/admin/auth/test` | Test auth endpoint | No |

## Implementation Details

### Controller Location
- **File:** `/workspaces/HRAPP/src/HRMS.API/Controllers/SetupController.cs`
- **Namespace:** `HRMS.API.Controllers`
- **Route:** `api/admin/setup`

### Dependencies
- `MasterDbContext` - For database access
- `IPasswordHasher` - For password hashing (Argon2)
- `ILogger<SetupController>` - For logging

### Key Features
✅ Checks if admin exists before creating
✅ Uses Argon2id for secure password hashing
✅ Comprehensive logging
✅ Proper error handling
✅ Returns detailed responses
✅ Environment-specific reset endpoint

## Next Steps

After setting up the first admin:

1. **Create Tenants:**
   - Use Admin Portal to create tenant organizations
   - Assign industry sectors from 30+ options

2. **Configure Tenants:**
   - Set up tenant-specific settings
   - Configure working hours, holidays, etc.

3. **Add Tenant Admins:**
   - Create HR/Admin users for each tenant
   - Assign appropriate roles

4. **Onboard Employees:**
   - Use Tenant Portal to add employees
   - Set up departments and designations

5. **Configure Modules:**
   - Set up attendance rules
   - Configure leave types and balances
   - Set up payroll components

## Support

For issues or questions:
- Check the logs in the console
- Review the API response messages
- Verify database connection
- Check appsettings.json configuration

---

**Generated:** 2025-11-01
**Status:** ✅ Ready for Testing
**Version:** 1.0.0
