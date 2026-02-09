namespace HoHemaLoans.Api.Services;

/// <summary>
/// Local file system implementation of document storage
/// Stores documents in wwwroot/documents/{userId}/{documentType}/{filename}
/// </summary>
public class LocalFileStorageService : IDocumentStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly long _maxFileSizeBytes = 10 * 1024 * 1024; // 10MB max
    private readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx"
    };

    public LocalFileStorageService(
        IWebHostEnvironment environment,
        ILogger<LocalFileStorageService> logger)
    {
        _basePath = Path.Combine(environment.ContentRootPath, "documents");
        _logger = logger;
        
        // Create base directory if it doesn't exist
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Created documents directory: {BasePath}", _basePath);
        }
    }

    public async Task<string> UploadDocumentAsync(IFormFile file, string userId, string documentType)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null");
            }

            if (file.Length > _maxFileSizeBytes)
            {
                throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSizeBytes / (1024 * 1024)}MB");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!_allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"File type {extension} is not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
            }

            // Create user-specific directory structure
            var userDirectory = Path.Combine(_basePath, userId, documentType);
            if (!Directory.Exists(userDirectory))
            {
                Directory.CreateDirectory(userDirectory);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            var relativePath = Path.Combine(userId, documentType, fileName);
            var fullPath = Path.Combine(_basePath, relativePath);

            // Save file
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("Document uploaded successfully: {FilePath}", relativePath);
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for user {UserId}", userId);
            throw;
        }
    }

    public async Task<string> ConvertToBase64Async(IFormFile file)
    {
        try
        {
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();
                return Convert.ToBase64String(fileBytes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting file to BASE64");
            throw;
        }
    }

    public async Task<Stream> GetDocumentAsync(string filePath)
    {
        try
        {
            var fullPath = GetFullPath(filePath);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Document not found: {filePath}");
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            
            return memory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document: {FilePath}", filePath);
            throw;
        }
    }

    public Task<bool> DeleteDocumentAsync(string filePath)
    {
        try
        {
            var fullPath = GetFullPath(filePath);
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Document deleted: {FilePath}", filePath);
                return Task.FromResult(true);
            }
            
            _logger.LogWarning("Document not found for deletion: {FilePath}", filePath);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document: {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        var fullPath = GetFullPath(filePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public string GetFullPath(string filePath)
    {
        return Path.Combine(_basePath, filePath);
    }
}
