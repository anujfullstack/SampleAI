namespace NLToSQLApp.DTOs;

/// <summary>
/// Request DTO for natural language queries
/// </summary>
public class QueryRequest
{
    /// <summary>
    /// Project identifier
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// Natural language query
    /// </summary>
    public string Query { get; set; } = string.Empty;
}