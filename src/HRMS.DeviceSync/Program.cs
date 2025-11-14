using HRMS.DeviceSync;
using HRMS.DeviceSync.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog for logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/device-sync-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Services.AddSerilog();

// Register services
builder.Services.AddHttpClient<HrmsApiClient>();
builder.Services.AddTransient<ZKTecoDeviceService>();

// Register worker service
builder.Services.AddHostedService<Worker>();

// Configure as Windows Service (optional)
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "HRMS Device Sync Service";
});

var host = builder.Build();

Log.Information("═══════════════════════════════════════════");
Log.Information("  HRMS Device Sync Service v1.0");
Log.Information("  For morishr.com");
Log.Information("═══════════════════════════════════════════");
Log.Information("Starting service...");

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.Information("Service stopped");
    Log.CloseAndFlush();
}
