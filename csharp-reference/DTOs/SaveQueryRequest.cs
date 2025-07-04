namespace NLToSQLApp.DTOs;

/// <summary>
/// Request DTO for saving a query
/// </summary>
public class SaveQueryRequest
{
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
    /// User identifier
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the query returned data
    /// </summary>
    public bool HasData { get; set; }

    /// <summary>
    /// Number of tokens used for this query
    /// </summary>
    public int TokensUsed { get; set; }

    /// <summary>
    /// Actual tokens used (for token logging calculation)
    /// </summary>
    public int ActualTokensUsed { get; set; }

    /// <summary>
    /// Event type for token logging
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Request DTO for updating feedback
/// </summary>
public class FeedbackRequest
{
    /// <summary>
    /// Feedback value (true = thumbs up, false = thumbs down)
    /// </summary>
    public bool ThumbsUp { get; set; }
}

/// <summary>
/// Response DTO for paginated results
/// </summary>
/// <typeparam name="T">Type of data</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>
    /// List of data items
    /// </summary>
    public List<T> Data { get; set; } = new();

    /// <summary>
    /// Total number of items
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
}