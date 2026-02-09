namespace HoHemaLoans.Api.Services;

/// <summary>
/// Interface for document storage operations
/// Supports local file system storage (for now) and can be extended to Azure Blob Storage
/// </summary>
public interface IDocumentStorageService
{
    /// <summary>
    /// Upload a document and return the file path
    /// </summary>
    Task<string> UploadDocumentAsync(IFormFile file, string userId, string documentType);
    
    /// <summary>
    /// Convert a file to BASE64 string
    /// </summary>
    Task<string> ConvertToBase64Async(IFormFile file);
    
    /// <summary>
    /// Get a document's file stream for download
    /// </summary>
    Task<Stream> GetDocumentAsync(string filePath);
    
    /// <summary>
    /// Delete a document from storage
    /// </summary>
    Task<bool> DeleteDocumentAsync(string filePath);
    
    /// <summary>
    /// Check if a file exists
    /// </summary>
    Task<bool> FileExistsAsync(string filePath);
    
    /// <summary>
    /// Get the full path for a document
    /// </summary>
    string GetFullPath(string filePath);
}
