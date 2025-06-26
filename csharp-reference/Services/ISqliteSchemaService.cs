using NLToSQLApp.Models;

namespace NLToSQLApp.Services;

/// <summary>
/// Service for SQLite database schema extraction and query execution
/// </summary>
public interface ISqliteSchemaService
{
    /// <summary>
    /// Extract database schema from SQLite file
    /// </summary>
    /// <param name="dbPath">Path to the SQLite database file</param>
    /// <returns>Dictionary containing table schemas</returns>
    Task<Dictionary<string, List<ColumnInfo>>> ExtractSchemaAsync(string dbPath);

    /// <summary>
    /// Execute SQL query on SQLite database
    /// </summary>
    /// <param name="dbPath">Path to the SQLite database file</param>
    /// <param name="sql">SQL query to execute</param>
    /// <returns>List of query results as dictionaries</returns>
    Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string dbPath, string sql);
}