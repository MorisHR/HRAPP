namespace HRMS.Core.Interfaces;

public interface IDatabaseBackupService
{
    /// <summary>
    /// Creates a backup of the master database
    /// </summary>
    /// <returns>The file path of the created backup</returns>
    Task<string> CreateBackupAsync();

    /// <summary>
    /// Restores the master database from a backup file
    /// </summary>
    /// <param name="backupFilePath">The path to the backup file</param>
    Task RestoreBackupAsync(string backupFilePath);

    /// <summary>
    /// Cleans up old backup files, keeping only the most recent ones
    /// </summary>
    /// <param name="keepCount">Number of recent backups to keep (default: 30)</param>
    Task CleanupOldBackupsAsync(int keepCount = 30);

    /// <summary>
    /// Lists all available backup files
    /// </summary>
    /// <returns>List of backup file paths ordered by creation time (newest first)</returns>
    Task<List<string>> ListBackupsAsync();
}
