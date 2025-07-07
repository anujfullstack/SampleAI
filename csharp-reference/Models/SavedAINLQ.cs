namespace MeetApp.AzureOpenAIHub.Models
{
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
        /// User identifier who created the query
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Application identifier
        /// </summary>
        public int ApplicationId { get; set; }

        /// <summary>
        /// Event identifier
        /// </summary>
        public int EventId { get; set; }

        /// <summary>
        /// User-defined label for the query
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Original natural language query
        /// </summary>
        public string NQLQuestion { get; set; } = string.Empty;

        /// <summary>
        /// Generated SQL query
        /// </summary>
        public string GeneratedSQL { get; set; } = string.Empty;

        /// <summary>
        /// Whether the query returned data (true = has data, false = no data)
        /// </summary>
        public bool HasData { get; set; }

        /// <summary>
        /// Number of tokens used for this query
        /// </summary>
        public int TokensUsed { get; set; }

        /// <summary>
        /// User feedback (true = thumbs up, false = thumbs down, null = no feedback)
        /// </summary>
        public bool? ThumbsUp { get; set; }

        /// <summary>
        /// When the query was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the query was last used
        /// </summary>
        public DateTime? LastUsed { get; set; }

        /// <summary>
        /// When feedback was provided
        /// </summary>
        public DateTime? FeedbackTimestamp { get; set; }

        /// <summary>
        /// When the query was deleted (soft delete)
        /// </summary>
        public DateTime? DeletedAt { get; set; }

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

        /// <summary>
        /// Whether the query is active (soft delete flag)
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether this is a sample query for users to try
        /// </summary>
        public bool IsSampleQuery { get; set; } = false;

        /// <summary>
        /// Category for grouping queries (e.g., "Participants", "Analytics", "Advanced")
        /// </summary>
        public string? Category { get; set; }
    }

    /// <summary>
    /// Usage statistics for saved queries
    /// </summary>
    public class SavedQueryStats
    {
        public int TotalQueries { get; set; }
        public int SuccessfulQueries { get; set; }
        public int QueriesWithPositiveFeedback { get; set; }
        public int QueriesWithNegativeFeedback { get; set; }
        public int TotalTokensUsed { get; set; }
        public int TotalUseCount { get; set; }
        public DateTime? LastQueryDate { get; set; }
        public string? MostUsedQuery { get; set; }
    }
}