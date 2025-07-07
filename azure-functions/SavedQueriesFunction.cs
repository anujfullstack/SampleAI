using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using Dapper;
using System.Collections.Generic;
using System.Linq;

namespace SavedQueriesAzureFunction
{
    public static class SavedQueriesFunction
    {
        private static readonly string ConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

        [FunctionName("SaveQuery")]
        public static async Task<IActionResult> SaveQuery(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "saved-queries")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("SaveQuery function triggered");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<SaveQueryRequest>(requestBody);

                if (request == null || string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.NQLQuestion))
                {
                    return new BadRequestObjectResult("Invalid request data");
                }

                var savedQuery = new SavedAINLQ
                {
                    UserId = request.UserId,
                    ApplicationId = request.ApplicationId,
                    EventId = request.EventId,
                    Label = request.Label ?? "Untitled Query",
                    NQLQuestion = request.NQLQuestion,
                    GeneratedSQL = request.GeneratedSQL ?? "",
                    HasData = request.HasData,
                    TokensUsed = request.TokensUsed,
                    ThumbsUp = request.ThumbsUp,
                    RequestId = request.RequestId ?? Guid.NewGuid().ToString(),
                    EventType = request.EventType ?? "NLQ_GENERATION",
                    Metadata = request.Metadata,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var insertSql = @"
                    INSERT INTO SavedAINLQ (
                        UserId, ApplicationId, EventId, Label, NQLQuestion, GeneratedSQL, 
                        HasData, TokensUsed, ThumbsUp, RequestId, EventType, Metadata, 
                        CreatedAt, LastUsed, UseCount, IsActive, IsSampleQuery
                    ) VALUES (
                        @UserId, @ApplicationId, @EventId, @Label, @NQLQuestion, @GeneratedSQL,
                        @HasData, @TokensUsed, @ThumbsUp, @RequestId, @EventType, @Metadata,
                        @CreatedAt, @LastUsed, @UseCount, @IsActive, @IsSampleQuery
                    );
                    SELECT CAST(SCOPE_IDENTITY() as int);";

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
                    savedQuery.RequestId,
                    savedQuery.EventType,
                    savedQuery.Metadata,
                    savedQuery.CreatedAt,
                    LastUsed = (DateTime?)null,
                    UseCount = 0,
                    savedQuery.IsActive,
                    IsSampleQuery = false
                };

                var newId = await connection.QuerySingleAsync<int>(insertSql, parameters);
                savedQuery.Id = newId;

                log.LogInformation($"Query saved successfully with ID: {newId}");

                return new OkObjectResult(new { success = true, id = newId, data = savedQuery });
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error saving query");
                return new StatusCodeResult(500);
            }
        }

        [FunctionName("GetUserQueries")]
        public static async Task<IActionResult> GetUserQueries(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "saved-queries/user/{userId}")] HttpRequest req,
            string userId,
            ILogger log)
        {
            try
            {
                log.LogInformation($"GetUserQueries function triggered for user: {userId}");

                var applicationId = req.Query["applicationId"].ToString();
                var hasData = req.Query["hasData"].ToString();
                var thumbsUp = req.Query["thumbsUp"].ToString();
                var searchTerm = req.Query["searchTerm"].ToString();
                var pageNumber = int.TryParse(req.Query["pageNumber"], out var page) ? page : 1;
                var pageSize = int.TryParse(req.Query["pageSize"], out var size) ? size : 20;

                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var whereConditions = new List<string> { "UserId = @UserId", "IsActive = 1", "IsSampleQuery = 0" };
                var parameters = new Dictionary<string, object> { { "UserId", userId } };

                if (!string.IsNullOrEmpty(applicationId) && int.TryParse(applicationId, out var appId))
                {
                    whereConditions.Add("ApplicationId = @ApplicationId");
                    parameters.Add("ApplicationId", appId);
                }

                if (!string.IsNullOrEmpty(hasData) && bool.TryParse(hasData, out var hasDataBool))
                {
                    whereConditions.Add("HasData = @HasData");
                    parameters.Add("HasData", hasDataBool);
                }

                if (!string.IsNullOrEmpty(thumbsUp) && bool.TryParse(thumbsUp, out var thumbsUpBool))
                {
                    whereConditions.Add("ThumbsUp = @ThumbsUp");
                    parameters.Add("ThumbsUp", thumbsUpBool);
                }

                if (!string.IsNullOrEmpty(searchTerm))
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

                var response = new
                {
                    success = true,
                    data = queries,
                    totalCount,
                    pageNumber,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error getting queries for user: {userId}");
                return new StatusCodeResult(500);
            }
        }

        [FunctionName("GetSampleQueries")]
        public static async Task<IActionResult> GetSampleQueries(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "sample-queries")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("GetSampleQueries function triggered");

                var applicationId = req.Query["applicationId"].ToString();
                var category = req.Query["category"].ToString();
                var limit = int.TryParse(req.Query["limit"], out var limitValue) ? limitValue : 50;

                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var whereConditions = new List<string> { "IsSampleQuery = 1", "IsActive = 1" };
                var parameters = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(applicationId) && int.TryParse(applicationId, out var appId))
                {
                    whereConditions.Add("ApplicationId = @ApplicationId");
                    parameters.Add("ApplicationId", appId);
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

                return new OkObjectResult(new { success = true, data = sampleQueries });
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error getting sample queries");
                return new StatusCodeResult(500);
            }
        }

        [FunctionName("UpdateFeedback")]
        public static async Task<IActionResult> UpdateFeedback(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "saved-queries/{id}/feedback")] HttpRequest req,
            int id,
            ILogger log)
        {
            try
            {
                log.LogInformation($"UpdateFeedback function triggered for query ID: {id}");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<FeedbackRequest>(requestBody);

                if (request == null)
                {
                    return new BadRequestObjectResult("Invalid request data");
                }

                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var updateSql = @"
                    UPDATE SavedAINLQ 
                    SET ThumbsUp = @ThumbsUp, 
                        FeedbackTimestamp = @FeedbackTimestamp,
                        LastUsed = @LastUsed
                    WHERE Id = @Id AND IsActive = 1";

                var parameters = new
                {
                    Id = id,
                    ThumbsUp = request.ThumbsUp,
                    FeedbackTimestamp = DateTime.UtcNow,
                    LastUsed = DateTime.UtcNow
                };

                var rowsAffected = await connection.ExecuteAsync(updateSql, parameters);

                if (rowsAffected == 0)
                {
                    return new NotFoundObjectResult("Query not found");
                }

                log.LogInformation($"Feedback updated for query ID: {id}, ThumbsUp: {request.ThumbsUp}");

                return new OkObjectResult(new { success = true, message = "Feedback updated successfully" });
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error updating feedback for query ID: {id}");
                return new StatusCodeResult(500);
            }
        }

        [FunctionName("UpdateUsage")]
        public static async Task<IActionResult> UpdateUsage(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "saved-queries/{id}/usage")] HttpRequest req,
            int id,
            ILogger log)
        {
            try
            {
                log.LogInformation($"UpdateUsage function triggered for query ID: {id}");

                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var updateSql = @"
                    UPDATE SavedAINLQ 
                    SET UseCount = UseCount + 1,
                        LastUsed = @LastUsed
                    WHERE Id = @Id AND IsActive = 1";

                var parameters = new
                {
                    Id = id,
                    LastUsed = DateTime.UtcNow
                };

                var rowsAffected = await connection.ExecuteAsync(updateSql, parameters);

                if (rowsAffected == 0)
                {
                    return new NotFoundObjectResult("Query not found");
                }

                log.LogInformation($"Usage updated for query ID: {id}");

                return new OkObjectResult(new { success = true, message = "Usage updated successfully" });
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error updating usage for query ID: {id}");
                return new StatusCodeResult(500);
            }
        }

        [FunctionName("GetQueryStats")]
        public static async Task<IActionResult> GetQueryStats(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "saved-queries/stats/{userId}")] HttpRequest req,
            string userId,
            ILogger log)
        {
            try
            {
                log.LogInformation($"GetQueryStats function triggered for user: {userId}");

                var applicationId = req.Query["applicationId"].ToString();

                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var whereClause = "UserId = @UserId AND IsActive = 1 AND IsSampleQuery = 0";
                var parameters = new Dictionary<string, object> { { "UserId", userId } };

                if (!string.IsNullOrEmpty(applicationId) && int.TryParse(applicationId, out var appId))
                {
                    whereClause += " AND ApplicationId = @ApplicationId";
                    parameters.Add("ApplicationId", appId);
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

                var response = new
                {
                    success = true,
                    data = new
                    {
                        TotalQueries = stats.TotalQueries ?? 0,
                        SuccessfulQueries = stats.SuccessfulQueries ?? 0,
                        QueriesWithPositiveFeedback = stats.QueriesWithPositiveFeedback ?? 0,
                        QueriesWithNegativeFeedback = stats.QueriesWithNegativeFeedback ?? 0,
                        TotalTokensUsed = stats.TotalTokensUsed ?? 0,
                        TotalUseCount = stats.TotalUseCount ?? 0,
                        LastQueryDate = stats.LastQueryDate,
                        MostUsedQuery = mostUsedQuery
                    }
                };

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error getting stats for user: {userId}");
                return new StatusCodeResult(500);
            }
        }

        [FunctionName("DeleteQuery")]
        public static async Task<IActionResult> DeleteQuery(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "saved-queries/{id}")] HttpRequest req,
            int id,
            ILogger log)
        {
            try
            {
                log.LogInformation($"DeleteQuery function triggered for query ID: {id}");

                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                // Soft delete
                var updateSql = @"
                    UPDATE SavedAINLQ 
                    SET IsActive = 0, 
                        DeletedAt = @DeletedAt
                    WHERE Id = @Id AND IsActive = 1";

                var parameters = new
                {
                    Id = id,
                    DeletedAt = DateTime.UtcNow
                };

                var rowsAffected = await connection.ExecuteAsync(updateSql, parameters);

                if (rowsAffected == 0)
                {
                    return new NotFoundObjectResult("Query not found");
                }

                log.LogInformation($"Query deleted successfully: {id}");

                return new OkObjectResult(new { success = true, message = "Query deleted successfully" });
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error deleting query ID: {id}");
                return new StatusCodeResult(500);
            }
        }
    }

    // DTOs
    public class SaveQueryRequest
    {
        public string UserId { get; set; }
        public int ApplicationId { get; set; }
        public int EventId { get; set; }
        public string Label { get; set; }
        public string NQLQuestion { get; set; }
        public string GeneratedSQL { get; set; }
        public bool HasData { get; set; }
        public int TokensUsed { get; set; }
        public bool? ThumbsUp { get; set; }
        public string RequestId { get; set; }
        public string EventType { get; set; }
        public string Metadata { get; set; }
    }

    public class FeedbackRequest
    {
        public bool ThumbsUp { get; set; }
    }

    public class SavedAINLQ
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ApplicationId { get; set; }
        public int EventId { get; set; }
        public string Label { get; set; }
        public string NQLQuestion { get; set; }
        public string GeneratedSQL { get; set; }
        public bool HasData { get; set; }
        public int TokensUsed { get; set; }
        public bool? ThumbsUp { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsed { get; set; }
        public DateTime? FeedbackTimestamp { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int UseCount { get; set; }
        public string RequestId { get; set; }
        public string EventType { get; set; }
        public string Metadata { get; set; }
        public bool IsActive { get; set; }
        public bool IsSampleQuery { get; set; }
        public string Category { get; set; }
    }
}