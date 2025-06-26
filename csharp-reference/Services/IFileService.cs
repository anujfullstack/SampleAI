namespace NLToSQLApp.Services;

/// <summary>
/// Service for handling file operations with Azure File Share
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Download SQLite database file from Azure File Share
    /// </summary>
    /// <param name="projectId">The project identifier</param>
    /// <returns>Local path to the downloaded database file</returns>
    Task<string> DownloadDatabaseAsync(int projectId);

    /// <summary>
    /// Clean up temporary files
    /// </summary>
    /// <param name="filePath">Path to the file to clean up</param>
    Task CleanupAsync(string filePath);
}