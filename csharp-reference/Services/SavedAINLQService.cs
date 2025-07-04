using Dapper;
using Microsoft.Data.Sqlite;
using NLToSQLApp.Models;
using NLToSQLApp.Services;
using System.Text.Json;

namespace NLToSQLApp.Services;

/// <summary>
/// Implementation of saved AI Natural Language Query service using Dapper
/// </summary>
public class SavedAINLQService : ISavedAINLQService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SavedAINLQService> _logger;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly string _connectionString;

    public SavedAINLQService(
        IConfiguration configuration,
        ILogger<SavedAINLQService> logger,
        ITokenUsageService tokenUsageService)
    {
        _configuration = configuration;
        _logger = logger;
        _tokenUsageService = tokenUsageService;
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection not found");
        
        InitializeDatabaseAsync().Wait();
    }

    /// <summary>
    /// Initialize the SavedAINLQ table if it doesn't exist
    /// </summary>
    private async Task InitializeDatabaseAsync()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS SavedAINLQ (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Label TEXT NOT NULL,
                    Query TEXT NOT NULL,
                    Sql TEXT NOT NULL,
                    ApplicationId INTEGER NOT NULL,
                    EventId INTEGER NOT NULL,
                    UserId TEXT NOT NULL,
                    HasData INTEGER NOT NULL DEFAULT 0,
                    ThumbsUp INTEGER NULL,
                    TokensUsed INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    LastUsed TEXT NULL,
                    UseCount INTEGER NOT NULL DEFAULT 0,
                    RequestId TEXT NULL,
                    EventType TEXT NOT NULL DEFAULT 'NLQ_GENERATION',
                    TokenLoggingSuccess INTEGER NOT NULL DEFAULT 1,
                    Metadata TEXT NULL
                );

                CREATE INDEX IF NOT EXISTS IX_SavedAINLQ_UserId ON SavedAINLQ(UserId);
                CREATE INDEX IF NOT EXISTS IX_SavedAINLQ_ApplicationId ON SavedAINLQ(ApplicationId);
                CREATE INDEX IF NOT EXISTS IX_SavedAINLQ_CreatedAt ON SavedAINLQ(CreatedAt);
                CREATE INDEX IF NOT EXISTS IX_SavedAINLQ_UseCount ON SavedAINLQ(UseCount);
            ";

            await connection.ExecuteAsync(createTableSql);
            _logger.LogInformation("SavedAINLQ table initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SavedAINLQ table");
            throw;
        }
    }

    /// <summary>
    /// Insert a new saved query with token usage logging
    /// </summary>
    public async Task<SavedAINLQ> InsertAsync(SavedAINLQ savedQuery, int actualTokensUsed = 0)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Step 1: Generate request ID if not provided
            var requestId = savedQuery.RequestId ?? Guid.NewGuid().ToString();
            savedQuery.RequestId = requestId;

            // Step 2: Determine event type (you can customize this based on your EventTypeConfig)
            var eventType = savedQuery.EventType ?? "NLQ_GENERATION";

            // Step 3: Log actual token usage (reusing your existing pattern)
            var tokensToLog = actualTokensUsed > 0 ? actualTokensUsed : savedQuery.TokensUsed;
            
            try
            {
                await _tokenUsageService.LogTokenUsageAsync(
                    savedQuery.ApplicationId, 
                    savedQuery.EventId, 
                    savedQuery.UserId, 
                    tokensToLog,
                    eventType, 
                    true, 
                    null, 
                    requestId);

                savedQuery.TokenLoggingSuccess = true;
                savedQuery.TokensUsed = tokensToLog;

                _logger.LogInformation(
                    "Token usage logged successfully for Application={ApplicationId}, Event={EventId}, Tokens={TokensUsed}",
                    savedQuery.ApplicationId, savedQuery.EventId, tokensToLog);
            }
            catch (Exception tokenEx)
            {
                _logger.LogWarning(tokenEx, "Failed to log token usage, but continuing with query save");
                savedQuery.TokenLoggingSuccess = false;
            }

            // Step 4: Get updated usage stats
            try
            {
                var updatedStats = await _tokenUsageService.GetUsageStatsAsync(savedQuery.ApplicationId);
                _logger.LogInformation("Updated usage stats retrieved for Application={ApplicationId}", savedQuery.ApplicationId);
            }
            catch (Exception statsEx)
            {
                _logger.LogWarning(statsEx, "Failed to retrieve updated usage stats");
            }

            // Step 5: Insert the saved query
            var insertSql = @"
                INSERT INTO SavedAINLQ (
                    Label, Query, Sql, ApplicationId, EventId, UserId, HasData, ThumbsUp, 
                    TokensUsed, CreatedAt, LastUsed, UseCount, RequestId, EventType, 
                    TokenLoggingSuccess, Metadata
                ) VALUES (
                    @Label, @Query, @Sql, @ApplicationId, @EventId, @UserId, @HasData, @ThumbsUp,
                    @TokensUsed, @CreatedAt, @LastUsed, @UseCount, @RequestId, @EventType,
                    @TokenLoggingSuccess, @Metadata
                );
                SELECT last_insert_rowid();
            ";

            var parameters = new
            {
                savedQuery.Label,
                savedQuery.Query,
                savedQuery.Sql,
                savedQuery.ApplicationId,
                savedQuery.EventId,
                savedQuery.UserId,
                HasData = savedQuery.HasData ? 1 : 0,
                ThumbsUp = savedQuery.ThumbsUp.HasValue ? (savedQuery.ThumbsUp.Value ? 1 : 0) : (int?)null,
                savedQuery.TokensUsed,
                CreatedAt = savedQuery.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                LastUsed = savedQuery.LastUsed?.ToString("yyyy-MM-dd HH:mm:ss"),
                savedQuery.UseCount,
                savedQuery.RequestId,
                EventType = eventType,
                TokenLoggingSuccess = savedQuery.TokenLoggingSuccess ? 1 : 0,
                savedQuery.Metadata
            };

            var newId = await connection.QuerySingleAsync<long>(insertSql, parameters);
            savedQuery.Id = (int)newId;

            _logger.LogInformation(
                "Saved query inserted successfully with ID={Id} for User={UserId}, Application={ApplicationId}",
                savedQuery.Id, savedQuery.UserId, savedQuery.ApplicationId);

            return savedQuery;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert saved query for User={UserId}", savedQuery.UserId);
            throw;
        }
    }

    /// <summary>
    /// Update an existing saved query
    /// </summary>
    public async Task<bool> UpdateAsync(SavedAINLQ savedQuery)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var updateSql = @"
                UPDATE SavedAINLQ SET
                    Label = @Label,
                    Query = @Query,
                    Sql = @Sql,
                    ApplicationId = @ApplicationId,
                    EventId = @EventId,
                    UserId = @UserId,
                    HasData = @HasData,
                    ThumbsUp = @ThumbsUp,
                    TokensUsed = @TokensUsed,
                    LastUsed = @LastUsed,
                    UseCount = @UseCount,
                    EventType = @EventType,
                    TokenLoggingSuccess = @TokenLoggingSuccess,
                    Metadata = @Metadata
                WHERE Id = @Id
            ";

            var parameters = new
            {
                savedQuery.Id,
                savedQuery.Label,
                savedQuery.Query,
                savedQuery.Sql,
                savedQuery.ApplicationId,
                savedQuery.EventId,
                savedQuery.UserId,
                HasData = savedQuery.HasData ? 1 : 0,
                ThumbsUp = savedQuery.ThumbsUp.HasValue ? (savedQuery.ThumbsUp.Value ? 1 : 0) : (int?)null,
                savedQuery.TokensUsed,
                LastUsed = savedQuery.LastUsed?.ToString("yyyy-MM-dd HH:mm:ss"),
                savedQuery.UseCount,
                savedQuery.EventType,
                TokenLoggingSuccess = savedQuery.TokenLoggingSuccess ? 1 : 0,
                savedQuery.Metadata
            };

            var rowsAffected = await connection.ExecuteAsync(updateSql, parameters);
            
            _logger.LogInformation("Updated saved query ID={Id}, RowsAffected={RowsAffected}", 
                savedQuery.Id, rowsAffected);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update saved query ID={Id}", savedQuery.Id);
            throw;
        }
    }

    /// <summary>
    /// Delete a saved query by ID
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var deleteSql = "DELETE FROM SavedAINLQ WHERE Id = @Id";
            var rowsAffected = await connection.ExecuteAsync(deleteSql, new { Id = id });

            _logger.LogInformation("Deleted saved query ID={Id}, RowsAffected={RowsAffected}", 
                id, rowsAffected);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete saved query ID={Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Get a saved query by ID
    /// </summary>
    public async Task<SavedAINLQ?> GetByIdAsync(int id)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var selectSql = "SELECT * FROM SavedAINLQ WHERE Id = @Id";
            var result = await connection.QuerySingleOrDefaultAsync<dynamic>(selectSql, new { Id = id });

            return result != null ? MapFromDynamic(result) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get saved query by ID={Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Get all saved queries for a user
    /// </summary>
    public async Task<List<SavedAINLQ>> GetByUserIdAsync(string userId, int? applicationId = null)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var selectSql = @"
                SELECT * FROM SavedAINLQ 
                WHERE UserId = @UserId
                AND (@ApplicationId IS NULL OR ApplicationId = @ApplicationId)
                ORDER BY CreatedAt DESC
            ";

            var results = await connection.QueryAsync<dynamic>(selectSql, new { UserId = userId, ApplicationId = applicationId });
            return results.Select(MapFromDynamic).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get saved queries for User={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get saved queries with pagination and filtering
    /// </summary>
    public async Task<(List<SavedAINLQ> Queries, int TotalCount)> GetPaginatedAsync(
        string userId,
        int? applicationId = null,
        bool? hasData = null,
        bool? thumbsUp = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var whereConditions = new List<string> { "UserId = @UserId" };
            var parameters = new Dictionary<string, object> { { "UserId", userId } };

            if (applicationId.HasValue)
            {
                whereConditions.Add("ApplicationId = @ApplicationId");
                parameters.Add("ApplicationId", applicationId.Value);
            }

            if (hasData.HasValue)
            {
                whereConditions.Add("HasData = @HasData");
                parameters.Add("HasData", hasData.Value ? 1 : 0);
            }

            if (thumbsUp.HasValue)
            {
                whereConditions.Add("ThumbsUp = @ThumbsUp");
                parameters.Add("ThumbsUp", thumbsUp.Value ? 1 : 0);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereConditions.Add("(Label LIKE @SearchTerm OR Query LIKE @SearchTerm OR Sql LIKE @SearchTerm)");
                parameters.Add("SearchTerm", $"%{searchTerm}%");
            }

            var whereClause = string.Join(" AND ", whereConditions);
            var offset = (pageNumber - 1) * pageSize;

            // Get total count
            var countSql = $"SELECT COUNT(*) FROM SavedAINLQ WHERE {whereClause}";
            var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

            // Get paginated results
            var selectSql = $@"
                SELECT * FROM SavedAINLQ 
                WHERE {whereClause}
                ORDER BY CreatedAt DESC
                LIMIT @PageSize OFFSET @Offset
            ";

            parameters.Add("PageSize", pageSize);
            parameters.Add("Offset", offset);

            var results = await connection.QueryAsync<dynamic>(selectSql, parameters);
            var queries = results.Select(MapFromDynamic).ToList();

            return (queries, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get paginated saved queries for User={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Update feedback for a saved query
    /// </summary>
    public async Task<bool> UpdateFeedbackAsync(int id, bool thumbsUp)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var updateSql = "UPDATE SavedAINLQ SET ThumbsUp = @ThumbsUp WHERE Id = @Id";
            var rowsAffected = await connection.ExecuteAsync(updateSql, new { Id = id, ThumbsUp = thumbsUp ? 1 : 0 });

            _logger.LogInformation("Updated feedback for saved query ID={Id}, ThumbsUp={ThumbsUp}", 
                id, thumbsUp);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update feedback for saved query ID={Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Update usage statistics when a query is reused
    /// </summary>
    public async Task<bool> UpdateUsageAsync(int id)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var updateSql = @"
                UPDATE SavedAINLQ SET 
                    UseCount = UseCount + 1,
                    LastUsed = @LastUsed
                WHERE Id = @Id
            ";

            var rowsAffected = await connection.ExecuteAsync(updateSql, new 
            { 
                Id = id, 
                LastUsed = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") 
            });

            _logger.LogInformation("Updated usage for saved query ID={Id}", id);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update usage for saved query ID={Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Get usage statistics for a user
    /// </summary>
    public async Task<SavedQueryStats> GetUsageStatsAsync(string userId, int? applicationId = null)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var whereClause = "UserId = @UserId";
            var parameters = new Dictionary<string, object> { { "UserId", userId } };

            if (applicationId.HasValue)
            {
                whereClause += " AND ApplicationId = @ApplicationId";
                parameters.Add("ApplicationId", applicationId.Value);
            }

            var statsSql = $@"
                SELECT 
                    COUNT(*) as TotalQueries,
                    SUM(CASE WHEN HasData = 1 THEN 1 ELSE 0 END) as SuccessfulQueries,
                    SUM(CASE WHEN ThumbsUp = 1 THEN 1 ELSE 0 END) as QueriesWithPositiveFeedback,
                    SUM(CASE WHEN ThumbsUp = 0 THEN 1 ELSE 0 END) as QueriesWithNegativeFeedback,
                    SUM(TokensUsed) as TotalTokensUsed,
                    SUM(UseCount) as TotalUseCount,
                    MAX(CreatedAt) as LastQueryDate
                FROM SavedAINLQ 
                WHERE {whereClause}
            ";

            var stats = await connection.QuerySingleAsync<dynamic>(statsSql, parameters);

            // Get most used query
            var mostUsedSql = $@"
                SELECT Label FROM SavedAINLQ 
                WHERE {whereClause} AND UseCount > 0
                ORDER BY UseCount DESC, CreatedAt DESC
                LIMIT 1
            ";

            var mostUsedQuery = await connection.QuerySingleOrDefaultAsync<string>(mostUsedSql, parameters);

            return new SavedQueryStats
            {
                TotalQueries = stats.TotalQueries ?? 0,
                SuccessfulQueries = stats.SuccessfulQueries ?? 0,
                QueriesWithPositiveFeedback = stats.QueriesWithPositiveFeedback ?? 0,
                QueriesWithNegativeFeedback = stats.QueriesWithNegativeFeedback ?? 0,
                TotalTokensUsed = stats.TotalTokensUsed ?? 0,
                TotalUseCount = stats.TotalUseCount ?? 0,
                LastQueryDate = !string.IsNullOrEmpty(stats.LastQueryDate) ? DateTime.Parse(stats.LastQueryDate) : null,
                MostUsedQuery = mostUsedQuery
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage stats for User={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get most popular queries across all users
    /// </summary>
    public async Task<List<SavedAINLQ>> GetPopularQueriesAsync(int? applicationId = null, int limit = 10)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var whereClause = applicationId.HasValue ? "WHERE ApplicationId = @ApplicationId" : "";
            var parameters = applicationId.HasValue ? new { ApplicationId = applicationId.Value, Limit = limit } : new { Limit = limit };

            var selectSql = $@"
                SELECT * FROM SavedAINLQ 
                {whereClause}
                ORDER BY UseCount DESC, CreatedAt DESC
                LIMIT @Limit
            ";

            var results = await connection.QueryAsync<dynamic>(selectSql, parameters);
            return results.Select(MapFromDynamic).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get popular queries");
            throw;
        }
    }

    /// <summary>
    /// Map dynamic result to SavedAINLQ object
    /// </summary>
    private static SavedAINLQ MapFromDynamic(dynamic result)
    {
        return new SavedAINLQ
        {
            Id = result.Id,
            Label = result.Label ?? string.Empty,
            Query = result.Query ?? string.Empty,
            Sql = result.Sql ?? string.Empty,
            ApplicationId = result.ApplicationId,
            EventId = result.EventId,
            UserId = result.UserId ?? string.Empty,
            HasData = result.HasData == 1,
            ThumbsUp = result.ThumbsUp == null ? null : result.ThumbsUp == 1,
            TokensUsed = result.TokensUsed,
            CreatedAt = DateTime.Parse(result.CreatedAt),
            LastUsed = !string.IsNullOrEmpty(result.LastUsed) ? DateTime.Parse(result.LastUsed) : null,
            UseCount = result.UseCount,
            RequestId = result.RequestId,
            EventType = result.EventType ?? "NLQ_GENERATION",
            TokenLoggingSuccess = result.TokenLoggingSuccess == 1,
            Metadata = result.Metadata
        };
    }
}