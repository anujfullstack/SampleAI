using Microsoft.AspNetCore.Mvc;
using NLToSQLApp.DTOs;
using NLToSQLApp.Models;
using NLToSQLApp.Services;

namespace NLToSQLApp.Controllers;

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
    /// <param name="request">The save request</param>
    /// <returns>The saved query</returns>
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
                Query = request.Query,
                Sql = request.Sql,
                ApplicationId = request.ApplicationId,
                EventId = request.EventId,
                UserId = request.UserId,
                HasData = request.HasData,
                TokensUsed = request.TokensUsed,
                EventType = request.EventType ?? "NLQ_GENERATION",
                Metadata = request.Metadata
            };

            var result = await _savedAINLQService.InsertAsync(savedQuery, request.ActualTokensUsed);

            _logger.LogInformation("Query saved successfully with ID={Id}", result.Id);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving query for User={UserId}", request.UserId);
            return StatusCode(500, "An error occurred while saving the query");
        }
    }

    /// <summary>
    /// Get saved queries for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="applicationId">Optional application ID filter</param>
    /// <param name="hasData">Optional filter by whether query has data</param>
    /// <param name="thumbsUp">Optional filter by feedback</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <returns>Paginated list of saved queries</returns>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<PaginatedResponse<SavedAINLQ>>> GetUserQueriesAsync(
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

            var response = new PaginatedResponse<SavedAINLQ>
            {
                Data = queries,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queries for User={UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving queries");
        }
    }

    /// <summary>
    /// Get a specific saved query by ID
    /// </summary>
    /// <param name="id">Query ID</param>
    /// <returns>The saved query</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<SavedAINLQ>> GetQueryByIdAsync(int id)
    {
        try
        {
            var query = await _savedAINLQService.GetByIdAsync(id);
            
            if (query == null)
            {
                return NotFound($"Query with ID {id} not found");
            }

            return Ok(query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting query by ID={Id}", id);
            return StatusCode(500, "An error occurred while retrieving the query");
        }
    }

    /// <summary>
    /// Update feedback for a saved query
    /// </summary>
    /// <param name="id">Query ID</param>
    /// <param name="request">Feedback request</param>
    /// <returns>Success status</returns>
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
                return NotFound($"Query with ID {id} not found");
            }

            return Ok(new { message = "Feedback updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feedback for Query={Id}", id);
            return StatusCode(500, "An error occurred while updating feedback");
        }
    }

    /// <summary>
    /// Update usage statistics when a query is reused
    /// </summary>
    /// <param name="id">Query ID</param>
    /// <returns>Success status</returns>
    [HttpPatch("{id}/usage")]
    public async Task<ActionResult> UpdateUsageAsync(int id)
    {
        try
        {
            _logger.LogInformation("Updating usage for Query={Id}", id);

            var success = await _savedAINLQService.UpdateUsageAsync(id);
            
            if (!success)
            {
                return NotFound($"Query with ID {id} not found");
            }

            return Ok(new { message = "Usage updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating usage for Query={Id}", id);
            return StatusCode(500, "An error occurred while updating usage");
        }
    }

    /// <summary>
    /// Delete a saved query
    /// </summary>
    /// <param name="id">Query ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteQueryAsync(int id)
    {
        try
        {
            _logger.LogInformation("Deleting Query={Id}", id);

            var success = await _savedAINLQService.DeleteAsync(id);
            
            if (!success)
            {
                return NotFound($"Query with ID {id} not found");
            }

            return Ok(new { message = "Query deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Query={Id}", id);
            return StatusCode(500, "An error occurred while deleting the query");
        }
    }

    /// <summary>
    /// Get usage statistics for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="applicationId">Optional application ID filter</param>
    /// <returns>Usage statistics</returns>
    [HttpGet("stats/{userId}")]
    public async Task<ActionResult<SavedQueryStats>> GetUsageStatsAsync(
        string userId, 
        [FromQuery] int? applicationId = null)
    {
        try
        {
            _logger.LogInformation("Getting usage stats for User={UserId}", userId);

            var stats = await _savedAINLQService.GetUsageStatsAsync(userId, applicationId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage stats for User={UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving usage statistics");
        }
    }

    /// <summary>
    /// Get popular queries across all users
    /// </summary>
    /// <param name="applicationId">Optional application ID filter</param>
    /// <param name="limit">Number of queries to return (default: 10)</param>
    /// <returns>List of popular queries</returns>
    [HttpGet("popular")]
    public async Task<ActionResult<List<SavedAINLQ>>> GetPopularQueriesAsync(
        [FromQuery] int? applicationId = null,
        [FromQuery] int limit = 10)
    {
        try
        {
            _logger.LogInformation("Getting popular queries, Limit={Limit}", limit);

            var queries = await _savedAINLQService.GetPopularQueriesAsync(applicationId, limit);
            return Ok(queries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular queries");
            return StatusCode(500, "An error occurred while retrieving popular queries");
        }
    }
}