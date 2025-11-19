# HANGFIRE BACKGROUND JOBS - QUICK START GUIDE

## 1. Enable Hangfire Dashboard (Development)

Edit `/workspaces/HRAPP/src/HRMS.API/appsettings.json`:
```json
"Hangfire": {
  "WorkerCount": 5,
  "DashboardEnabled": true,        // Change this to true
  "RequireAuthentication": false   // Optional: Set to false for easier dev access
}
```

## 2. Start the Application

```bash
cd /workspaces/HRAPP/src/HRMS.API
dotnet run
```

## 3. Access Hangfire Dashboard

Open your browser:
```
http://localhost:5000/hangfire
```

## 4. Verify Background Jobs

You should see 5 recurring jobs:

| Job Name | Schedule | Next Run |
|----------|----------|----------|
| daily-mv-refresh | Daily 3:00 AM UTC | Check dashboard |
| daily-token-cleanup | Daily 4:00 AM UTC | Check dashboard |
| weekly-vacuum-maintenance | Sunday 4:00 AM UTC | Check dashboard |
| monthly-partition-maintenance | 1st of month 2:00 AM UTC | Check dashboard |
| daily-health-check | Daily 6:00 AM UTC | Check dashboard |

## 5. Test a Job Manually

1. In the dashboard, click "Recurring Jobs" tab
2. Find `daily-health-check` (fastest job, ~1 minute timeout)
3. Click "Trigger now" button
4. Watch the "Jobs" tab to see execution
5. Check "Succeeded" to see completion

## 6. View Logs

Application logs will show:
```
[Information] Starting daily database health check...
[Information] Database Health OK - ...
[Information] Daily database health check completed. All systems healthy.
```

## Troubleshooting

**Dashboard not loading?**
- Verify `DashboardEnabled: true` in appsettings.json
- Check application is running
- Try: http://localhost:5000/hangfire

**No jobs showing?**
- Check startup logs for "Database maintenance jobs scheduled"
- Verify PostgreSQL is running: `pg_isready -h localhost -p 5432`

**Jobs failing?**
- Ensure database stored procedures exist (run migrations)
- Check connection string in appsettings.json
- Review job logs in dashboard

## Database Prerequisites

Ensure these stored procedures exist in your database:
- `master.refresh_all_materialized_views_corrected()`
- `master.cleanup_expired_refresh_tokens()`
- `master.weekly_vacuum_maintenance()`
- `master.monthly_partition_maintenance()`
- `master.database_health_check()`

Run migrations if needed:
```bash
cd /workspaces/HRAPP/src/HRMS.API
dotnet ef database update
```

## Full Documentation

See `HANGFIRE_VERIFICATION_REPORT.md` for complete details.
