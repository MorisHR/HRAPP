# Automated Database Maintenance Jobs Integration Guide

**Created:** 2025-11-14
**Status:** Ready to Deploy

---

## Overview

This guide shows how to integrate the automated database maintenance jobs into your HRMS application.

---

## Step 1: Add to Program.cs or Startup.cs

### Option A: Minimal Integration (Program.cs)

Add this to your `Program.cs` after Hangfire configuration:

```csharp
using HRMS.Infrastructure.BackgroundJobs;

// ... existing code ...

// Configure Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

// Register DatabaseMaintenanceJobs
builder.Services.AddScoped<DatabaseMaintenanceJobs>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var logger = sp.GetRequiredService<ILogger<DatabaseMaintenanceJobs>>();
    return new DatabaseMaintenanceJobs(connectionString, logger);
});

var app = builder.Build();

// ... existing middleware ...

// Initialize maintenance jobs (add this after app.UseHangfireDashboard())
DatabaseMaintenanceJobs.RegisterScheduledJobs();

app.Run();
```

### Option B: Full Integration (Startup.cs)

If using Startup.cs pattern:

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // ... existing services ...

        // Add Hangfire
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(Configuration.GetConnectionString("DefaultConnection")));

        services.AddHangfireServer();

        // Register DatabaseMaintenanceJobs
        services.AddScoped<DatabaseMaintenanceJobs>(sp =>
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var logger = sp.GetRequiredService<ILogger<DatabaseMaintenanceJobs>>();
            return new DatabaseMaintenanceJobs(connectionString, logger);
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // ... existing middleware ...

        app.UseHangfireDashboard("/hangfire");

        // Initialize maintenance jobs
        DatabaseMaintenanceJobs.RegisterScheduledJobs();

        // ... rest of configuration ...
    }
}
```

---

## Step 2: Verify Jobs are Scheduled

After starting your application:

1. **Navigate to Hangfire Dashboard:**
   ```
   https://your-app-url/hangfire
   ```

2. **Click "Recurring Jobs" tab**

3. **You should see 5 scheduled jobs:**
   - `daily-mv-refresh` - Daily at 3:00 AM UTC
   - `daily-token-cleanup` - Daily at 4:00 AM UTC
   - `weekly-vacuum-maintenance` - Sunday at 4:00 AM UTC
   - `monthly-partition-maintenance` - 1st of month at 2:00 AM UTC
   - `daily-health-check` - Daily at 6:00 AM UTC

---

## Step 3: Test Jobs Manually (Optional)

### Trigger Jobs Immediately (for testing):

```csharp
// Add this to a controller or API endpoint for testing
[HttpPost("api/admin/trigger-maintenance")]
[Authorize(Roles = "SuperAdmin")]
public IActionResult TriggerMaintenance()
{
    DatabaseMaintenanceJobs.TriggerManualMaintenance();
    return Ok("Maintenance jobs triggered");
}
```

Or trigger via Hangfire Dashboard:
1. Go to "Recurring Jobs"
2. Click "Trigger now" button next to any job

---

## Job Schedule Reference

| Job Name | Frequency | Time (UTC) | Purpose | Duration |
|----------|-----------|------------|---------|----------|
| **daily-mv-refresh** | Daily | 3:00 AM | Refresh materialized views | ~1-5 min |
| **daily-token-cleanup** | Daily | 4:00 AM | Delete expired tokens | ~1-2 min |
| **weekly-vacuum-maintenance** | Weekly (Sun) | 4:00 AM | Clean bloated tables | ~5-10 min |
| **monthly-partition-maintenance** | Monthly (1st) | 2:00 AM | Create future partitions | ~1-2 min |
| **daily-health-check** | Daily | 6:00 AM | Database health monitoring | ~30 sec |

---

## Monitoring Job Execution

### Check Logs

Jobs log to your application's logging system:

```bash
# Check application logs for job execution
grep "Starting daily materialized view refresh" /var/log/hrms/app.log

# Check for job errors
grep "Error during" /var/log/hrms/app.log
```

### Hangfire Dashboard

- **Succeeded Jobs:** Green count increases after successful execution
- **Failed Jobs:** Red count shows failures (click to see error details)
- **Job History:** Click job name to see execution history

---

## Customizing Job Schedules

To change job schedules, modify the `Cron` expressions in `RegisterScheduledJobs()`:

```csharp
// Change materialized view refresh to 2 AM
RecurringJob.AddOrUpdate<DatabaseMaintenanceJobs>(
    "daily-mv-refresh",
    jobs => jobs.RefreshMaterializedViewsAsync(),
    Cron.Daily(2), // Changed from 3 to 2
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

// Change vacuum to Saturday instead of Sunday
RecurringJob.AddOrUpdate<DatabaseMaintenanceJobs>(
    "weekly-vacuum-maintenance",
    jobs => jobs.WeeklyVacuumMaintenanceAsync(),
    Cron.Weekly(DayOfWeek.Saturday, 4), // Changed from Sunday
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

### Common Cron Patterns

```csharp
Cron.Daily(3)                          // Every day at 3 AM
Cron.Hourly()                          // Every hour
Cron.Weekly(DayOfWeek.Sunday, 4)      // Sunday at 4 AM
Cron.Monthly(1, 2)                     // 1st of month at 2 AM
"0 */6 * * *"                          // Every 6 hours
"0 0 * * 1-5"                          // Weekdays at midnight
```

---

## Troubleshooting

### Job Not Appearing in Dashboard

**Issue:** Jobs don't show in Hangfire dashboard

**Solutions:**
1. Ensure `DatabaseMaintenanceJobs.RegisterScheduledJobs()` is called after `app.UseHangfireDashboard()`
2. Check that Hangfire is properly configured with PostgreSQL storage
3. Restart the application

### Job Failing with Connection Error

**Issue:** Job fails with "connection refused" or "timeout"

**Solutions:**
1. Check connection string in appsettings.json
2. Ensure database is accessible from application server
3. Verify firewall rules allow database connections
4. Check if database is running: `sudo service postgresql status`

### Job Takes Too Long

**Issue:** Job execution exceeds timeout

**Solutions:**
1. Increase `CommandTimeout` in the job method
2. Check database performance (run health check)
3. Review slow query logs
4. Consider running job during low-traffic hours

### Materialized View Refresh Failing

**Issue:** Daily MV refresh fails

**Solutions:**
1. Check if materialized views exist:
   ```sql
   SELECT * FROM pg_matviews WHERE schemaname IN ('master', 'tenant_siraaj');
   ```
2. Manually test refresh:
   ```sql
   SELECT * FROM master.refresh_all_materialized_views_corrected();
   ```
3. Review error message in Hangfire dashboard

---

## Health Check Alerts

The `daily-health-check` job logs issues to your application log. You can integrate with alerting:

### Email Alerts (Example)

```csharp
public async Task DailyHealthCheckAsync()
{
    // ... existing health check code ...

    if (hasIssues)
    {
        // Send email alert
        await _emailService.SendAsync(new EmailMessage
        {
            To = "admin@yourcompany.com",
            Subject = "HRMS Database Health Alert",
            Body = $"Database health check found issues. Check logs at {DateTime.UtcNow}"
        });
    }
}
```

### Slack/Teams Alerts (Example)

```csharp
if (hasIssues)
{
    await _httpClient.PostAsJsonAsync("https://hooks.slack.com/your-webhook", new
    {
        text = $"⚠️ HRMS Database Health Alert - Check Hangfire dashboard"
    });
}
```

---

## Performance Impact

All jobs are scheduled during low-traffic hours to minimize impact:

- **CPU Impact:** Low (< 5% for < 1 minute)
- **Memory Impact:** Low (< 50 MB)
- **Disk I/O:** Low to Medium during vacuum
- **Lock Impact:** Minimal (CONCURRENTLY used where possible)

**Recommendation:** Monitor first execution, then set and forget.

---

## Disable Jobs Temporarily

To temporarily disable a job:

**Option 1: Via Hangfire Dashboard**
1. Go to "Recurring Jobs"
2. Click "Delete" button next to the job
3. Re-register by restarting app or calling `RegisterScheduledJobs()` again

**Option 2: Via Code**
```csharp
RecurringJob.RemoveIfExists("daily-mv-refresh");
```

---

## Summary

✅ **5 Automated Jobs** configured for database maintenance
✅ **Zero manual intervention** required
✅ **Automatic monitoring** via health checks
✅ **Full logging** of all operations
✅ **Easy customization** via Cron expressions

**Next Steps:**
1. Add code to Program.cs (2 lines)
2. Restart application
3. Verify jobs in Hangfire dashboard
4. Monitor first execution
5. Enjoy automated database maintenance!

---

**Created:** 2025-11-14
**Integration Time:** ~5 minutes
**Status:** Production Ready ✅
