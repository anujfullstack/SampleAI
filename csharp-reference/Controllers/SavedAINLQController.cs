using Microsoft.AspNetCore.Mvc;
using MeetApp.AzureOpenAIHub.Models;
using MeetApp.AzureOpenAIHub.Services;

namespace MeetApp.AzureOpenAIHub.Controllers
{
    /// <summary>
    /// Controller for managing saved AI Natural Language Queries
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SavedAINLQController : ControllerBase
    {
        private readonly ISavedAINLQService _savedAINLQService;
        private readonly ILogger<SavedAINLQController> _logger;

        public SavedAINLQController(
            ISavedAINLQService savedAINLQService,
            ILogger<SavedAINLQController> logger)
        {
            _savedAINLQService = savedAINLQService;
            _logger = logger;
        }

        /// <summary>
        /// Save a new AI Natural Language Query
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SavedAINLQ>> SaveQueryAsync([FromBody] SaveQueryRequest request)
        {
            try
            {
                _logger.LogInformation("Saving query for User={UserId}, Application={ApplicationId}", 
                    request.UserId, request.ApplicationId);

                var savedQuery = new SavedAINLQ
                {
                    Label = request.Label,
                    NQLQuestion = request.NQLQuestion,
                    GeneratedSQL = request.GeneratedSQL,
                    ApplicationId = request.ApplicationId,
                    EventId = request.EventId,
                    UserId = request.UserId,
                    HasData = request.HasData,
                    TokensUsed = request.TokensUsed,
                    EventType = request.EventType ?? "NLQ_GENERATION",
                    Metadata = request.Metadata,
                    Category = request.Category
                };

                var result = await _savedAINLQService.InsertAsync(savedQuery, request.ActualTokensUsed);

                _logger.LogInformation("Query saved successfully with ID={Id}", result.Id);

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving query for User={UserId}", request.UserId);
                return StatusCode(500, new { success = false, error = "An error occurred while saving the query" });
            }
        }

        /// <summary>
        /// Get saved queries for a user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult> GetUserQueriesAsync(
            string userId,
            [FromQuery] int? applicationId = null,
            [FromQuery] bool? hasData = null,
            [FromQuery] bool? thumbsUp = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Getting queries for User={UserId}, Page={PageNumber}", 
                    userId, pageNumber);

                var (queries, totalCount) = await _savedAINLQService.GetPaginatedAsync(
                    userId, applicationId, hasData, thumbsUp, searchTerm, pageNumber, pageSize);

                var response = new
                {
                    success = true,
                    data = queries,
                    totalCount,
                    pageNumber,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queries for User={UserId}", userId);
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving queries" });
            }
        }

        /// <summary>
        /// Get sample queries
        /// </summary>
        [HttpGet("sample")]
        public async Task<ActionResult> GetSampleQueriesAsync(
            [FromQuery] int? applicationId = null,
            [FromQuery] string? category = null,
            [FromQuery] int limit = 50)
        {
            try
            {
                _logger.LogInformation("Getting sample queries, Limit={Limit}", limit);

                var queries = await _savedAINLQService.GetSampleQueriesAsync(applicationId, category, limit);
                return Ok(new { success = true, data = queries });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sample queries");
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving sample queries" });
            }
        }

        /// <summary>
        /// Update feedback for a saved query
        /// </summary>
        [HttpPatch("{id}/feedback")]
        public async Task<ActionResult> UpdateFeedbackAsync(int id, [FromBody] FeedbackRequest request)
        {
            try
            {
                _logger.LogInformation("Updating feedback for Query={Id}, ThumbsUp={ThumbsUp}", 
                    id, request.ThumbsUp);

                var success = await _savedAINLQService.UpdateFeedbackAsync(id, request.ThumbsUp);
                
                if (!success)
                {
                    return NotFound(new { success = false, error = $"Query with ID {id} not found" });
                }

                return Ok(new { success = true, message = "Feedback updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating feedback for Query={Id}", id);
                return StatusCode(500, new { success = false, error = "An error occurred while updating feedback" });
            }
        }

        /// <summary>
        /// Update usage statistics when a query is reused
        /// </summary>
        [HttpPatch("{id}/usage")]
        public async Task<ActionResult> UpdateUsageAsync(int id)
        {
            try
            {
                _logger.LogInformation("Updating usage for Query={Id}", id);

                var success = await _savedAINLQService.UpdateUsageAsync(id);
                
                if (!success)
                {
                    return NotFound(new { success = false, error = $"Query with ID {id} not found" });
                }

                return Ok(new { success = true, message = "Usage updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating usage for Query={Id}", id);
                return StatusCode(500, new { success = false, error = "An error occurred while updating usage" });
            }
        }

        /// <summary>
        /// Get usage statistics for a user
        /// </summary>
        [HttpGet("stats/{userId}")]
        public async Task<ActionResult> GetUsageStatsAsync(
            string userId, 
            [FromQuery] int? applicationId = null)
        {
            try
            {
                _logger.LogInformation("Getting usage stats for User={UserId}", userId);

                var stats = await _savedAINLQService.GetUsageStatsAsync(userId, applicationId);
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage stats for User={UserId}", userId);
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving usage statistics" });
            }
        }

        /// <summary>
        /// Delete a saved query
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteQueryAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting Query={Id}", id);

                var success = await _savedAINLQService.DeleteAsync(id);
                
                if (!success)
                {
                    return NotFound(new { success = false, error = $"Query with ID {id} not found" });
                }

                return Ok(new { success = true, message = "Query deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Query={Id}", id);
                return StatusCode(500, new { success = false, error = "An error occurred while deleting the query" });
            }
        }
    }

    /// <summary>
    /// Request DTO for saving a query
    /// </summary>
    public class SaveQueryRequest
    {
        public string Label { get; set; } = string.Empty;
        public string NQLQuestion { get; set; } = string.Empty;
        public string GeneratedSQL { get; set; } = string.Empty;
        public int ApplicationId { get; set; }
        public int EventId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public bool HasData { get; set; }
        public int TokensUsed { get; set; }
        public int ActualTokensUsed { get; set; }
        public string? EventType { get; set; }
        public string? Metadata { get; set; }
        public string? Category { get; set; }
    }

    /// <summary>
    /// Request DTO for updating feedback
    /// </summary>
    public class FeedbackRequest
    {
        public bool ThumbsUp { get; set; }
    }
}