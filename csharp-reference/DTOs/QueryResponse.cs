using NLToSQLApp.Models;

namespace NLToSQLApp.DTOs;

/// <summary>
/// Response DTO for query results
/// </summary>
public class QueryResponse
{
    /// <summary>
    /// Query result data
    /// </summary>
    public List<Dictionary<string, object>> Data { get; set; } = new();

    /// <summary>
    /// Generated SQL query
    /// </summary>
    public string Sql { get; set; } = string.Empty;

    /// <summary>
    /// Token usage information
    /// </summary>
    public TokenUsage? TokenUsage { get; set; }

    /// <summary>
    /// Database schema table names
    /// </summary>
    public List<string> Schema { get; set; } = new();
}