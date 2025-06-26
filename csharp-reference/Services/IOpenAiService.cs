using NLToSQLApp.Models;

namespace NLToSQLApp.Services;

/// <summary>
/// Service for Azure OpenAI integration
/// </summary>
public interface IOpenAiService
{
    /// <summary>
    /// Generate SQL query from natural language using Azure OpenAI
    /// </summary>
    /// <param name="schema">Database schema information</param>
    /// <param name="userQuery">User's natural language query</param>
    /// <returns>Generated SQL query with token usage information</returns>
    Task<SqlGenerationResult> GenerateSqlAsync(Dictionary<string, List<ColumnInfo>> schema, string userQuery);
}