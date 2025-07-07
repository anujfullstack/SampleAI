using System.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MeetApp.FunctionBiz;

namespace MeetApp.AzureOpenAIHub.DataManager
{
    public class SavedAINLQDataManagerHelper
    {
        private readonly ILogger<SavedAINLQDataManagerHelper> _logger;
        private readonly IConfiguration _configuration;
        private readonly ITokenUsageService _tokenUsageService;
        private readonly string _connectionString;

        public SavedAINLQDataManagerHelper(
            ILogger<SavedAINLQDataManagerHelper> logger,
            IConfiguration configuration,
            ITokenUsageService tokenUsageService)
        {
            _logger = logger;
            _configuration = configuration;
            _tokenUsageService = tokenUsageService;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection not found");
        }

        /// <summary>
        /// Insert a new saved query with token usage logging
        /// </summary>
        public async Task<SavedAINLQ> InsertSavedQueryAsync(SavedAINLQ savedQuery, int actualTokensUsed = 0)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                // Step 1: Generate request ID if not provided
                var requestId = savedQuery.RequestId ?? Guid.NewGuid().ToString();
                savedQuery.RequestId = requestId;

                // Step 2: Determine event type
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
                        UserId, ApplicationId, EventId, Label, NQLQuestion, GeneratedSQL, 
                        HasData, TokensUsed, ThumbsUp, CreatedAt, LastUsed, UseCount, 
                        RequestId, EventType, TokenLoggingSuccess, Metadata, IsActive, 
                        IsSampleQuery, Category, FeedbackTimestamp
                    ) VALUES (
                        @UserId, @ApplicationId, @EventId, @Label, @NQLQuestion, @GeneratedSQL,
                        @HasData, @TokensUsed, @ThumbsUp, @CreatedAt, @LastUsed, @UseCount,
                        @RequestId, @EventType, @TokenLoggingSuccess, @Metadata, @IsActive,
                        @IsSampleQuery, @Category, @FeedbackTimestamp
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS int);";

                var parameters = new
                {
                    savedQuery.UserId,
                    savedQuery.ApplicationId,
                    savedQuery.EventId,
                    savedQuery.Label,
                    savedQuery.NQLQuestion,
                    savedQuery.GeneratedSQL,
                    savedQuery.HasData,
                    savedQuery.TokensUsed,
                    savedQuery.ThumbsUp,
                    CreatedAt = savedQuery.CreatedAt,
                    LastUsed = savedQuery.LastUsed,
                    UseCount = savedQuery.UseCount,
                    savedQuery.RequestId,
                    EventType = eventType,
                    TokenLoggingSuccess = savedQuery.TokenLoggingSuccess,
                    savedQuery.Metadata,
                    IsActive = true,
                    IsSampleQuery = false,
                    savedQuery.Category,
                    savedQuery.FeedbackTimestamp
                };

                var newId = await connection.ExecuteScalarAsync<int>(insertSql, parameters, transaction);
                savedQuery.Id = newId;

                transaction.Commit();

                _logger.LogInformation(
                    "Saved query inserted successfully with ID={Id} for User={UserId}, Application={ApplicationId}",
                    savedQuery.Id, savedQuery.UserId, savedQuery.ApplicationId);

                return savedQuery;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Failed to insert saved query for User={UserId}", savedQuery.UserId);
                throw;
            }
        }

        /// <summary>
        /// Update an existing saved query
        /// </summary>
        public async Task<bool> UpdateSavedQueryAsync(SavedAINLQ savedQuery)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var updateSql = @"
                    UPDATE SavedAINLQ SET
                        Label = @Label,
                        NQLQuestion = @NQLQuestion,
                        GeneratedSQL = @GeneratedSQL,
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
                        Metadata = @Metadata,
                        Category = @Category,
                        FeedbackTimestamp = @FeedbackTimestamp
                    WHERE Id = @Id AND IsActive = 1";

                var parameters = new
                {
                    savedQuery.Id,
                    savedQuery.Label,
                    savedQuery.NQLQuestion,
                    savedQuery.GeneratedSQL,
                    savedQuery.ApplicationId,
                    savedQuery.EventId,
                    savedQuery.UserId,
                    savedQuery.HasData,
                    savedQuery.ThumbsUp,
                    savedQuery.TokensUsed,
                    savedQuery.LastUsed,
                    savedQuery.UseCount,
                    savedQuery.EventType,
                    savedQuery.TokenLoggingSuccess,
                    savedQuery.Metadata,
                    savedQuery.Category,
                    savedQuery.FeedbackTimestamp
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
        /// Soft delete a saved query by ID
        /// </summary>
        public async Task<bool> DeleteSavedQueryAsync(int id)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var deleteSql = @"
                    UPDATE SavedAINLQ 
                    SET IsActive = 0, 
                        DeletedAt = @DeletedAt
                    WHERE Id = @Id AND IsActive = 1";

                var rowsAffected = await connection.ExecuteAsync(deleteSql, new 
                { 
                    Id = id, 
                    DeletedAt = DateTime.UtcNow 
                });

                _logger.LogInformation("Soft deleted saved query ID={Id}, RowsAffected={RowsAffected}", 
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
        public async Task<SavedAINLQ?> GetSavedQueryByIdAsync(int id)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var selectSql = @"
                    SELECT * FROM SavedAINLQ 
                    WHERE Id = @Id AND IsActive = 1";

                var result = await connection.QuerySingleOrDefaultAsync<SavedAINLQ>(selectSql, new { Id = id });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get saved query by ID={Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Get saved queries with pagination and filtering
        /// </summary>
        public async Task<(List<SavedAINLQ> Queries, int TotalCount)> GetPaginatedSavedQueriesAsync(
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
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var whereConditions = new List<string> { "UserId = @UserId", "IsActive = 1", "IsSampleQuery = 0" };
                var parameters = new Dictionary<string, object> { { "UserId", userId } };

                if (applicationId.HasValue)
                {
                    whereConditions.Add("ApplicationId = @ApplicationId");
                    parameters.Add("ApplicationId", applicationId.Value);
                }

                if (hasData.HasValue)
                {
                    whereConditions.Add("HasData = @HasData");
                    parameters.Add("HasData", hasData.Value);
                }

                if (thumbsUp.HasValue)
                {
                    whereConditions.Add("ThumbsUp = @ThumbsUp");
                    parameters.Add("ThumbsUp", thumbsUp.Value);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    whereConditions.Add("(Label LIKE @SearchTerm OR NQLQuestion LIKE @SearchTerm OR GeneratedSQL LIKE @SearchTerm)");
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
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                parameters.Add("Offset", offset);
                parameters.Add("PageSize", pageSize);

                var queries = await connection.QueryAsync<SavedAINLQ>(selectSql, parameters);

                return (queries.ToList(), totalCount);
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
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var updateSql = @"
                    UPDATE SavedAINLQ 
                    SET ThumbsUp = @ThumbsUp, 
                        FeedbackTimestamp = @FeedbackTimestamp,
                        LastUsed = @LastUsed
                    WHERE Id = @Id AND IsActive = 1";

                var rowsAffected = await connection.ExecuteAsync(updateSql, new 
                { 
                    Id = id, 
                    ThumbsUp = thumbsUp,
                    FeedbackTimestamp = DateTime.UtcNow,
                    LastUsed = DateTime.UtcNow
                });

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
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var updateSql = @"
                    UPDATE SavedAINLQ 
                    SET UseCount = UseCount + 1,
                        LastUsed = @LastUsed
                    WHERE Id = @Id AND IsActive = 1";

                var rowsAffected = await connection.ExecuteAsync(updateSql, new 
                { 
                    Id = id, 
                    LastUsed = DateTime.UtcNow
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
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var whereClause = "UserId = @UserId AND IsActive = 1 AND IsSampleQuery = 0";
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
                    WHERE {whereClause}";

                var stats = await connection.QuerySingleAsync(statsSql, parameters);

                // Get most used query
                var mostUsedSql = $@"
                    SELECT TOP 1 Label FROM SavedAINLQ 
                    WHERE {whereClause} AND UseCount > 0
                    ORDER BY UseCount DESC, CreatedAt DESC";

                var mostUsedQuery = await connection.QuerySingleOrDefaultAsync<string>(mostUsedSql, parameters);

                return new SavedQueryStats
                {
                    TotalQueries = stats.TotalQueries ?? 0,
                    SuccessfulQueries = stats.SuccessfulQueries ?? 0,
                    QueriesWithPositiveFeedback = stats.QueriesWithPositiveFeedback ?? 0,
                    QueriesWithNegativeFeedback = stats.QueriesWithNegativeFeedback ?? 0,
                    TotalTokensUsed = stats.TotalTokensUsed ?? 0,
                    TotalUseCount = stats.TotalUseCount ?? 0,
                    LastQueryDate = stats.LastQueryDate,
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
        /// Get sample queries from database
        /// </summary>
        public async Task<List<SavedAINLQ>> GetSampleQueriesAsync(int? applicationId = null, string? category = null, int limit = 50)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var whereConditions = new List<string> { "IsSampleQuery = 1", "IsActive = 1" };
                var parameters = new Dictionary<string, object>();

                if (applicationId.HasValue)
                {
                    whereConditions.Add("ApplicationId = @ApplicationId");
                    parameters.Add("ApplicationId", applicationId.Value);
                }

                if (!string.IsNullOrEmpty(category))
                {
                    whereConditions.Add("Category = @Category");
                    parameters.Add("Category", category);
                }

                var whereClause = string.Join(" AND ", whereConditions);
                parameters.Add("Limit", limit);

                var selectSql = $@"
                    SELECT TOP (@Limit) * FROM SavedAINLQ 
                    WHERE {whereClause}
                    ORDER BY UseCount DESC, CreatedAt DESC";

                var sampleQueries = await connection.QueryAsync<SavedAINLQ>(selectSql, parameters);
                return sampleQueries.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get sample queries");
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
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var whereClause = "IsActive = 1 AND IsSampleQuery = 0";
                var parameters = new Dictionary<string, object> { { "Limit", limit } };

                if (applicationId.HasValue)
                {
                    whereClause += " AND ApplicationId = @ApplicationId";
                    parameters.Add("ApplicationId", applicationId.Value);
                }

                var selectSql = $@"
                    SELECT TOP (@Limit) * FROM SavedAINLQ 
                    WHERE {whereClause}
                    ORDER BY UseCount DESC, CreatedAt DESC";

                var queries = await connection.QueryAsync<SavedAINLQ>(selectSql, parameters);
                return queries.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get popular queries");
                throw;
            }
        }

        /// <summary>
        /// Insert sample queries into database
        /// </summary>
        public async Task<bool> InsertSampleQueryAsync(SavedAINLQ sampleQuery)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var insertSql = @"
                    INSERT INTO SavedAINLQ (
                        UserId, ApplicationId, EventId, Label, NQLQuestion, GeneratedSQL, 
                        HasData, TokensUsed, CreatedAt, UseCount, IsActive, IsSampleQuery, Category
                    ) VALUES (
                        @UserId, @ApplicationId, @EventId, @Label, @NQLQuestion, @GeneratedSQL,
                        @HasData, @TokensUsed, @CreatedAt, @UseCount, @IsActive, @IsSampleQuery, @Category
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS int);";

                var parameters = new
                {
                    UserId = "system",
                    sampleQuery.ApplicationId,
                    sampleQuery.EventId,
                    sampleQuery.Label,
                    sampleQuery.NQLQuestion,
                    sampleQuery.GeneratedSQL,
                    sampleQuery.HasData,
                    TokensUsed = 0,
                    CreatedAt = DateTime.UtcNow,
                    UseCount = 0,
                    IsActive = true,
                    IsSampleQuery = true,
                    sampleQuery.Category
                };

                var newId = await connection.ExecuteScalarAsync<int>(insertSql, parameters);
                
                _logger.LogInformation("Sample query inserted successfully with ID={Id}", newId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert sample query");
                throw;
            }
        }
    }
}