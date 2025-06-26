using Azure.Storage.Files.Shares;
using NLToSQLApp.Services;

namespace NLToSQLApp.Services;

/// <summary>
/// Implementation of file service for Azure File Share operations
/// </summary>
public class FileService : IFileService
{
    private readonly ShareClient _shareClient;
    private readonly ILogger<FileService> _logger;
    private readonly string _tempDirectory;

    public FileService(IConfiguration configuration, ILogger<FileService> logger)
    {
        var connectionString = configuration.GetConnectionString("AzureFileShare");
        var shareName = configuration["AzureFileShare:ShareName"];
        
        //_shareClient = new ShareClient(connectionString, shareName);
        _logger = logger;
        //_tempDirectory = Path.GetTempPath();
    }
    public async Task<string> DownloadDatabaseAsync(int projectId)
    {
        try
        {
            var fileName = $"project_{projectId}.db";
            var localPath = "C:\\AI-Projects\\Ventla-Working-Sample\\ProjectV1\\server\\" + fileName; // Or replace _tempDirectory with "./Databases"

            if (!File.Exists(localPath))
            {
                _logger.LogWarning("Local SQLite file not found at path: {LocalPath}", localPath);
                throw new FileNotFoundException($"Local database file not found for project {projectId}");
            }

            _logger.LogInformation("Using local SQLite database file: {LocalPath}", localPath);

            // Simulate async even though it's a local file
            await Task.CompletedTask;

            return localPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load local database file for project {ProjectId}", projectId);
            throw;
        }
    }
    /// <summary>
    /// Download SQLite database file from Azure File Share
    /// </summary>
    /// <param name="projectId">The project identifier</param>
    /// <returns>Local path to the downloaded database file</returns>
    public async Task<string> DownloadDatabaseAsync1(int projectId)
    {
        try
        {
            var fileName = $"project_{projectId}.db";
            var shareFileClient = _shareClient.GetRootDirectoryClient().GetFileClient(fileName);
            
            var localPath = Path.Combine(_tempDirectory, fileName);
            
            _logger.LogInformation("Downloading database file {FileName} to {LocalPath}", 
                fileName, localPath);

            var downloadResponse = await shareFileClient.DownloadAsync();
            
            using var fileStream = File.Create(localPath);
            await downloadResponse.Value.Content.CopyToAsync(fileStream);
            
            _logger.LogInformation("Successfully downloaded database file for project {ProjectId}", 
                projectId);

            return localPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download database for project {ProjectId}", projectId);
            throw new FileNotFoundException($"Database file for project {projectId} not found", ex);
        }
    }

    /// <summary>
    /// Clean up temporary files
    /// </summary>
    /// <param name="filePath">Path to the file to clean up</param>
    public async Task CleanupAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                //await Task.Run(() => File.Delete(filePath));
                _logger.LogInformation("Cleaned up temporary file: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up temporary file: {FilePath}", filePath);
        }
    }
}