using Google.Cloud.Storage.V1;
using HRMS.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Google Cloud Storage implementation for file storage
/// Handles file uploads, downloads, and deletions in production-grade cloud storage
/// </summary>
public class GoogleCloudStorageService : IFileStorageService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;
    private readonly ILogger<GoogleCloudStorageService> _logger;

    public GoogleCloudStorageService(
        IConfiguration configuration,
        ILogger<GoogleCloudStorageService> logger)
    {
        _logger = logger;

        // Get bucket name from configuration
        _bucketName = configuration["GoogleCloud:StorageBucket"]
            ?? throw new InvalidOperationException("GoogleCloud:StorageBucket configuration is missing");

        // Initialize Storage Client
        // In production, authentication happens automatically via:
        // - GOOGLE_APPLICATION_CREDENTIALS environment variable
        // - Default service account (on GCP)
        // - Workload identity (on GKE)
        try
        {
            _storageClient = StorageClient.Create();
            _logger.LogInformation("Google Cloud Storage client initialized. Bucket: {Bucket}", _bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Google Cloud Storage client");
            throw new InvalidOperationException(
                "Failed to initialize Google Cloud Storage. Ensure GOOGLE_APPLICATION_CREDENTIALS is set or running on GCP.",
                ex);
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder, string? contentType = null)
    {
        try
        {
            // Sanitize filename (remove dangerous characters)
            var sanitizedFileName = SanitizeFileName(fileName);

            // Generate unique filename to prevent collisions
            var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}";

            // Build object path: folder/unique-filename
            var objectName = string.IsNullOrWhiteSpace(folder)
                ? uniqueFileName
                : $"{folder.TrimEnd('/')}/{uniqueFileName}";

            // Auto-detect content type if not provided
            if (string.IsNullOrEmpty(contentType))
            {
                contentType = GetContentType(fileName);
            }

            _logger.LogInformation("Uploading file to GCS: {Bucket}/{ObjectName}", _bucketName, objectName);

            // Upload to Google Cloud Storage
            var uploadedObject = await _storageClient.UploadObjectAsync(
                bucket: _bucketName,
                objectName: objectName,
                contentType: contentType,
                source: fileStream);

            var cloudStoragePath = $"gs://{_bucketName}/{objectName}";

            _logger.LogInformation("File uploaded successfully: {Path}", cloudStoragePath);

            return cloudStoragePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} to folder {Folder}", fileName, folder);
            throw new InvalidOperationException($"Failed to upload file to cloud storage: {ex.Message}", ex);
        }
    }

    public async Task<Stream> DownloadFileAsync(string filePath)
    {
        try
        {
            var objectName = ExtractObjectName(filePath);

            _logger.LogInformation("Downloading file from GCS: {Bucket}/{ObjectName}", _bucketName, objectName);

            var memoryStream = new MemoryStream();
            await _storageClient.DownloadObjectAsync(_bucketName, objectName, memoryStream);

            memoryStream.Position = 0; // Reset stream position for reading

            _logger.LogInformation("File downloaded successfully: {ObjectName}", objectName);

            return memoryStream;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("File not found in GCS: {FilePath}", filePath);
            throw new FileNotFoundException($"File not found in cloud storage: {filePath}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FilePath}", filePath);
            throw new InvalidOperationException($"Failed to download file from cloud storage: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            var objectName = ExtractObjectName(filePath);

            _logger.LogInformation("Deleting file from GCS: {Bucket}/{ObjectName}", _bucketName, objectName);

            await _storageClient.DeleteObjectAsync(_bucketName, objectName);

            _logger.LogInformation("File deleted successfully: {ObjectName}", objectName);

            return true;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
            return false; // File doesn't exist, consider it deleted
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FilePath}", filePath);
            throw new InvalidOperationException($"Failed to delete file from cloud storage: {ex.Message}", ex);
        }
    }

    public async Task<string> GetSignedUrlAsync(string filePath, int expirationMinutes = 60)
    {
        try
        {
            var objectName = ExtractObjectName(filePath);

            _logger.LogInformation("Generating signed URL for: {Bucket}/{ObjectName}, Expiration: {Minutes}min",
                _bucketName, objectName, expirationMinutes);

            var urlSigner = UrlSigner.FromCredentialFile(
                Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS"));

            var signedUrl = await urlSigner.SignAsync(
                bucket: _bucketName,
                objectName: objectName,
                duration: TimeSpan.FromMinutes(expirationMinutes),
                httpMethod: HttpMethod.Get);

            _logger.LogInformation("Signed URL generated successfully");

            return signedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating signed URL for {FilePath}", filePath);
            throw new InvalidOperationException($"Failed to generate signed URL: {ex.Message}", ex);
        }
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        try
        {
            var objectName = ExtractObjectName(filePath);

            var obj = await _storageClient.GetObjectAsync(_bucketName, objectName);

            return obj != null;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence: {FilePath}", filePath);
            throw new InvalidOperationException($"Failed to check file existence: {ex.Message}", ex);
        }
    }

    // ==================== HELPER METHODS ====================

    /// <summary>
    /// Extracts object name from full GCS path (gs://bucket/path or https://...)
    /// </summary>
    private string ExtractObjectName(string filePath)
    {
        if (filePath.StartsWith("gs://"))
        {
            // Format: gs://bucket/path/to/file.ext
            var pathWithoutProtocol = filePath.Substring(5); // Remove "gs://"
            var firstSlashIndex = pathWithoutProtocol.IndexOf('/');
            if (firstSlashIndex > 0)
            {
                return pathWithoutProtocol.Substring(firstSlashIndex + 1);
            }
        }
        else if (filePath.StartsWith("https://storage.googleapis.com/"))
        {
            // Format: https://storage.googleapis.com/bucket/path/to/file.ext
            var pathWithoutProtocol = filePath.Substring(31); // Remove "https://storage.googleapis.com/"
            var firstSlashIndex = pathWithoutProtocol.IndexOf('/');
            if (firstSlashIndex > 0)
            {
                return pathWithoutProtocol.Substring(firstSlashIndex + 1);
            }
        }

        // Assume it's already just the object name
        return filePath;
    }

    /// <summary>
    /// Sanitizes filename by removing dangerous characters
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        // Remove path traversal attempts
        fileName = Path.GetFileName(fileName);

        // Remove or replace dangerous characters
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }

        // Remove additional dangerous characters for cloud storage
        fileName = fileName.Replace(' ', '_')
                           .Replace('&', '_')
                           .Replace('?', '_')
                           .Replace('#', '_');

        return fileName;
    }

    /// <summary>
    /// Gets MIME content type based on file extension
    /// </summary>
    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}
