using Microsoft.Data.Sqlite;
using NLToSQLApp.Models;
using NLToSQLApp.Services;

namespace NLToSQLApp.Services;

/// <summary>
/// Implementation of SQLite schema service
/// </summary>
public class SqliteSchemaService : ISqliteSchemaService
{
    private readonly ILogger<SqliteSchemaService> _logger;

    public SqliteSchemaService(ILogger<SqliteSchemaService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extract database schema from SQLite file
    /// </summary>
    /// <param name="dbPath">Path to the SQLite database file</param>
    /// <returns>Dictionary containing table schemas</returns>
    public async Task<Dictionary<string, List<ColumnInfo>>> ExtractSchemaAsync(string dbPath)
    {
        var schema = new Dictionary<string, List<ColumnInfo>>();
        
        try
        {
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            await connection.OpenAsync();

            // Get all table names
            var tables = new List<string>();
            var tableCommand = connection.CreateCommand();
            tableCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
            
            using var tableReader = await tableCommand.ExecuteReaderAsync();
            while (await tableReader.ReadAsync())
            {
                tables.Add(tableReader.GetString(0));
            }

            // Get column information for each table
            foreach (var tableName in tables)
            {
                var columns = new List<ColumnInfo>();
                var columnCommand = connection.CreateCommand();
                columnCommand.CommandText = $"PRAGMA table_info({tableName})";
                
                using var columnReader = await columnCommand.ExecuteReaderAsync();
                while (await columnReader.ReadAsync())
                {
                    columns.Add(new ColumnInfo
                    {
                        Name = columnReader.GetString(columnReader.GetOrdinal("name")),
                        Type = columnReader.GetString(columnReader.GetOrdinal("type")),
                        NotNull = columnReader.GetBoolean(columnReader.GetOrdinal("notnull")),
                        PrimaryKey = columnReader.GetBoolean(columnReader.GetOrdinal("pk"))
                    });
                }
                
                schema[tableName] = columns;
            }

            _logger.LogInformation("Extracted schema for {TableCount} tables from {DbPath}", 
                schema.Count, dbPath);

            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract schema from {DbPath}", dbPath);
            throw;
        }
    }

    /// <summary>
    /// Execute SQL query on SQLite database
    /// </summary>
    /// <param name="dbPath">Path to the SQLite database file</param>
    /// <param name="sql">SQL query to execute</param>
    /// <returns>List of query results as dictionaries</returns>
    public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string dbPath, string sql)
    {
        var results = new List<Dictionary<string, object>>();
        
        try
        {
            // Basic SQL injection prevention
            if (ContainsDangerousOperations(sql))
            {
                throw new InvalidOperationException("Query contains potentially dangerous operations");
            }

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = sql;
            
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columnName] = value;
                }
                
                results.Add(row);
            }

            _logger.LogInformation("Executed query and returned {RecordCount} records", results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute query: {Sql}", sql);
            throw;
        }
    }

    /// <summary>
    /// Check if SQL contains potentially dangerous operations
    /// </summary>
    /// <param name="sql">SQL query to check</param>
    /// <returns>True if dangerous operations are detected</returns>
    private static bool ContainsDangerousOperations(string sql)
    {
        var dangerousKeywords = new[] { "DROP", "DELETE", "TRUNCATE", "ALTER", "CREATE", "INSERT", "UPDATE" };
        var upperSql = sql.ToUpperInvariant();
        
        return dangerousKeywords.Any(keyword => upperSql.Contains(keyword));
    }
}