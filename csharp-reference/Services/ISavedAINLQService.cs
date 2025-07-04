using NLToSQLApp.Models;

namespace NLToSQLApp.Services;

/// <summary>
/// Service interface for managing saved AI Natural Language Queries
/// </summary>
public interface ISavedAINLQService
{
    /// <summary>
    /// Insert a new saved query with token usage logging
    /// </summary>
    /// <param name="savedQuery">The query to save</param>
    /// <param name="actualTokensUsed">Actual tokens used (0 if using default)</param>
    /// <returns>The saved query with generated ID</returns>
    Task<SavedAINLQ> InsertAsync(SavedAINLQ savedQuery, int actualTokensUsed = 0);

    /// <summary>
    /// Update an existing saved query
    /// </summary>
    /// <param name="savedQuery">The query to update</param>
    /// <returns>True if update was successful</returns>
    Task<bool> UpdateAsync(SavedAINLQ savedQuery);

    /// <summary>
    /// Delete a saved query by ID
    /// </summary>
    /// <param name="id">The ID of the query to delete</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Get a saved query by ID
    /// </summary>
    /// <param name="id">The ID of the query</param>
    /// <returns>The saved query or null if not found</returns>
    Task<SavedAINLQ?> GetByIdAsync(int id);

    /// <summary>
    /// Get all saved queries for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="applicationId">Optional application ID filter</param>
    /// <returns>List of saved queries</returns>
    Task<List<SavedAINLQ>> GetByUserIdAsync(string userId, int? applicationId = null);

    /// <summary>
    /// Get saved queries with pagination and filtering
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="applicationId">Optional application ID filter</param>
    /// <param name="hasData">Optional filter by whether query has data</param>
    /// <param name="thumbsUp">Optional filter by feedback</param>
    /// <param name="searchTerm">Optional search term for label/query</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of saved queries</returns>
    Task<(List<SavedAINLQ> Queries, int TotalCount)> GetPaginatedAsync(
        string userId,
        int? applicationId = null,
        bool? hasData = null,
        bool? thumbsUp = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 20);

    /// <summary>
    /// Update feedback for a saved query
    /// </summary>
    /// <param name="id">The query ID</param>
    /// <param name="thumbsUp">The feedback (true = thumbs up, false = thumbs down)</param>
    /// <returns>True if update was successful</returns>
    Task<bool> UpdateFeedbackAsync(int id, bool thumbsUp);

    /// <summary>
    /// Update usage statistics when a query is reused
    /// </summary>
    /// <param name="id">The query ID</param>
    /// <returns>True if update was successful</returns>
    Task<bool> UpdateUsageAsync(int id);

    /// <summary>
    /// Get usage statistics for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="applicationId">Optional application ID filter</param>
    /// <returns>Usage statistics</returns>
    Task<SavedQueryStats> GetUsageStatsAsync(string userId, int? applicationId = null);

    /// <summary>
    /// Get most popular queries across all users
    /// </summary>
    /// <param name="applicationId">Optional application ID filter</param>
    /// <param name="limit">Number of queries to return</param>
    /// <returns>List of popular queries</returns>
    Task<List<SavedAINLQ>> GetPopularQueriesAsync(int? applicationId = null, int limit = 10);
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