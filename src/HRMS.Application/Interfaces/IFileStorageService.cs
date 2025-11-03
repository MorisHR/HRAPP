namespace HRMS.Application.Interfaces;

/// <summary>
/// Service for cloud-based file storage operations
/// Abstracts storage implementation (Google Cloud Storage, AWS S3, Azure Blob, etc.)
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file to cloud storage
    /// </summary>
    /// <param name="fileStream">The file content stream</param>
    /// <param name="fileName">Original filename (will be sanitized)</param>
    /// <param name="folder">Virtual folder path (e.g., "leave-attachments/employee-id")</param>
    /// <param name="contentType">MIME content type (optional, auto-detected if null)</param>
    /// <returns>Cloud storage path (gs://bucket/path or https://storage.googleapis.com/...)</returns>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder, string? contentType = null);

    /// <summary>
    /// Downloads a file from cloud storage
    /// </summary>
    /// <param name="filePath">Cloud storage path (gs://bucket/path or full URL)</param>
    /// <returns>File content stream</returns>
    Task<Stream> DownloadFileAsync(string filePath);

    /// <summary>
    /// Deletes a file from cloud storage
    /// </summary>
    /// <param name="filePath">Cloud storage path to delete</param>
    /// <returns>True if deleted successfully, false if file doesn't exist</returns>
    Task<bool> DeleteFileAsync(string filePath);

    /// <summary>
    /// Generates a signed URL for temporary file access (expires after specified duration)
    /// </summary>
    /// <param name="filePath">Cloud storage path</param>
    /// <param name="expirationMinutes">URL expiration time in minutes (default: 60)</param>
    /// <returns>Signed URL for temporary access</returns>
    Task<string> GetSignedUrlAsync(string filePath, int expirationMinutes = 60);

    /// <summary>
    /// Checks if a file exists in cloud storage
    /// </summary>
    /// <param name="filePath">Cloud storage path</param>
    /// <returns>True if file exists, false otherwise</returns>
    Task<bool> FileExistsAsync(string filePath);
}
