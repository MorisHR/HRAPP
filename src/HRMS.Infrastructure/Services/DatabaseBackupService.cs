using HRMS.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HRMS.Infrastructure.Services;

public class DatabaseBackupService : IDatabaseBackupService
{
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _backupDirectory;
    private readonly string _connectionString;
    private readonly string _databaseName;
    private string _host = "localhost";
    private string _username = "postgres";
    private string _password = "postgres";

    public DatabaseBackupService(
        ILogger<DatabaseBackupService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Parse connection string
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        ParseConnectionString(_connectionString);

        _databaseName = "hrms_master";

        // Set backup directory
        var rootPath = Directory.GetCurrentDirectory();
        _backupDirectory = Path.Combine(rootPath, "backups", "database");

        // Ensure backup directory exists
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
            _logger.LogInformation("Created backup directory: {Directory}", _backupDirectory);
        }
    }

    private void ParseConnectionString(string connectionString)
    {
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            var keyValue = part.Split('=');
            if (keyValue.Length != 2) continue;

            var key = keyValue[0].Trim().ToLower();
            var value = keyValue[1].Trim();

            switch (key)
            {
                case "host":
                case "server":
                    _host = value;
                    break;
                case "username":
                case "user id":
                case "user":
                    _username = value;
                    break;
                case "password":
                    _password = value;
                    break;
            }
        }

        // Fallback to configuration if not in connection string
        _host ??= "localhost";
        _username ??= "postgres";
        _password ??= _configuration.GetConnectionString("Password") ?? "postgres";
    }

    public async Task<string> CreateBackupAsync()
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"hrms_backup_{timestamp}.sql";
            var backupFilePath = Path.Combine(_backupDirectory, backupFileName);

            _logger.LogInformation("Starting database backup to {FilePath}", backupFilePath);

            // Use pg_dump to create backup
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "pg_dump",
                Arguments = $"-h {_host} -U {_username} -d {_databaseName} -f \"{backupFilePath}\" --clean --if-exists --create",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Set password environment variable
            processStartInfo.EnvironmentVariables["PGPASSWORD"] = _password;

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Database backup failed. Exit code: {ExitCode}, Error: {Error}",
                    process.ExitCode, error);
                throw new Exception($"Database backup failed: {error}");
            }

            // Get file size
            var fileInfo = new FileInfo(backupFilePath);
            var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);

            _logger.LogInformation("Database backup completed successfully. File: {FilePath}, Size: {Size:F2} MB",
                backupFilePath, fileSizeMB);

            return backupFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating database backup");
            throw;
        }
    }

    public async Task RestoreBackupAsync(string backupFilePath)
    {
        try
        {
            if (!File.Exists(backupFilePath))
            {
                throw new FileNotFoundException($"Backup file not found: {backupFilePath}");
            }

            _logger.LogInformation("Starting database restore from {FilePath}", backupFilePath);

            // Use psql to restore backup
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "psql",
                Arguments = $"-h {_host} -U {_username} -d postgres -f \"{backupFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Set password environment variable
            processStartInfo.EnvironmentVariables["PGPASSWORD"] = _password;

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0 && !error.Contains("already exists"))
            {
                _logger.LogError("Database restore failed. Exit code: {ExitCode}, Error: {Error}",
                    process.ExitCode, error);
                throw new Exception($"Database restore failed: {error}");
            }

            _logger.LogInformation("Database restore completed successfully from {FilePath}", backupFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring database backup");
            throw;
        }
    }

    public async Task CleanupOldBackupsAsync(int keepCount = 30)
    {
        try
        {
            var backups = await ListBackupsAsync();

            if (backups.Count <= keepCount)
            {
                _logger.LogInformation("No old backups to clean up. Current count: {Count}, Keep count: {KeepCount}",
                    backups.Count, keepCount);
                return;
            }

            var backupsToDelete = backups.Skip(keepCount).ToList();

            foreach (var backup in backupsToDelete)
            {
                File.Delete(backup);
                _logger.LogInformation("Deleted old backup: {FilePath}", backup);
            }

            _logger.LogInformation("Cleaned up {Count} old backup(s)", backupsToDelete.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old backups");
            throw;
        }
    }

    public Task<List<string>> ListBackupsAsync()
    {
        try
        {
            if (!Directory.Exists(_backupDirectory))
            {
                return Task.FromResult(new List<string>());
            }

            var backupFiles = Directory.GetFiles(_backupDirectory, "hrms_backup_*.sql")
                .OrderByDescending(f => new FileInfo(f).CreationTimeUtc)
                .ToList();

            return Task.FromResult(backupFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing backups");
            throw;
        }
    }
}
