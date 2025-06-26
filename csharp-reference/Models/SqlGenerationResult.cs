namespace NLToSQLApp.Models;

/// <summary>
/// Result of SQL generation from OpenAI
/// </summary>
public class SqlGenerationResult
{
    /// <summary>
    /// Generated SQL query
    /// </summary>
    public string Sql { get; set; } = string.Empty;

    /// <summary>
    /// Token usage information
    /// </summary>
    public TokenUsage TokenUsage { get; set; } = new();
}