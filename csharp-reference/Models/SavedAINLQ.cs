namespace NLToSQLApp.Models;

/// <summary>
/// Represents a saved AI Natural Language Query
/// </summary>
public class SavedAINLQ
{
    /// <summary>
    /// Unique identifier for the saved query
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User-defined label for the query
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Original natural language query
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Generated SQL query
    /// </summary>
    public string Sql { get; set; } = string.Empty;

    /// <summary>
    /// Application identifier
    /// </summary>
    public int ApplicationId { get; set; }

    /// <summary>
    /// Event identifier
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// User identifier who created the query
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the query returned data (1 = has data, 0 = no data)
    /// </summary>
    public bool HasData { get; set; }

    /// <summary>
    /// User feedback (1 = thumbs up, 0 = thumbs down, null = no feedback)
    /// </summary>
    public bool? ThumbsUp { get; set; }

    /// <summary>
    /// Number of tokens used for this query
    /// </summary>
    public int TokensUsed { get; set; }

    /// <summary>
    /// When the query was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the query was last used
    /// </summary>
    public DateTime? LastUsed { get; set; }

    /// <summary>
    /// Number of times this query has been reused
    /// </summary>
    public int UseCount { get; set; } = 0;

    /// <summary>
    /// Request identifier for tracking
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Event type for token logging
    /// </summary>
    public string EventType { get; set; } = "NLQ_GENERATION";

    /// <summary>
    /// Whether the token logging was successful
    /// </summary>
    public bool TokenLoggingSuccess { get; set; } = true;

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }
}