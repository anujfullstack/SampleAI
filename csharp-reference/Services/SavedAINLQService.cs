using MeetApp.AzureOpenAIHub.DataManager;
using MeetApp.AzureOpenAIHub.Models;
using Microsoft.Extensions.Logging;

namespace MeetApp.AzureOpenAIHub.Services
{
    /// <summary>
    /// Service for managing saved AI Natural Language Queries
    /// </summary>
    public interface ISavedAINLQService
    {
        Task<SavedAINLQ> InsertAsync(SavedAINLQ savedQuery, int actualTokensUsed = 0);
        Task<bool> UpdateAsync(SavedAINLQ savedQuery);
        Task<bool> DeleteAsync(int id);
        Task<SavedAINLQ?> GetByIdAsync(int id);
        Task<(List<SavedAINLQ> Queries, int TotalCount)> GetPaginatedAsync(
            string userId, int? applicationId = null, bool? hasData = null, 
            bool? thumbsUp = null, string? searchTerm = null, 
            int pageNumber = 1, int pageSize = 20);
        Task<bool> UpdateFeedbackAsync(int id, bool thumbsUp);
        Task<bool> UpdateUsageAsync(int id);
        Task<SavedQueryStats> GetUsageStatsAsync(string userId, int? applicationId = null);
        Task<List<SavedAINLQ>> GetSampleQueriesAsync(int? applicationId = null, string? category = null, int limit = 50);
        Task<List<SavedAINLQ>> GetPopularQueriesAsync(int? applicationId = null, int limit = 10);
        Task<bool> InsertSampleQueryAsync(SavedAINLQ sampleQuery);
    }

    /// <summary>
    /// Implementation of saved AI Natural Language Query service using DataManagerHelper pattern
    /// </summary>
    public class SavedAINLQService : ISavedAINLQService
    {
        private readonly SavedAINLQDataManagerHelper _dataManagerHelper;
        private readonly ILogger<SavedAINLQService> _logger;

        public SavedAINLQService(
            SavedAINLQDataManagerHelper dataManagerHelper,
            ILogger<SavedAINLQService> logger)
        {
            _dataManagerHelper = dataManagerHelper;
            _logger = logger;
        }

        /// <summary>
        /// Insert a new saved query with token usage logging
        /// </summary>
        public async Task<SavedAINLQ> InsertAsync(SavedAINLQ savedQuery, int actualTokensUsed = 0)
        {
            try
            {
                _logger.LogInformation("Inserting saved query for User={UserId}, Application={ApplicationId}", 
                    savedQuery.UserId, savedQuery.ApplicationId);

                return await _dataManagerHelper.InsertSavedQueryAsync(savedQuery, actualTokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavedAINLQService.InsertAsync for User={UserId}", savedQuery.UserId);
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
                _logger.LogInformation("Updating saved query ID={Id}", savedQuery.Id);
                return await _dataManagerHelper.UpdateSavedQueryAsync(savedQuery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavedAINLQService.UpdateAsync for ID={Id}", savedQuery.Id);
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
                _logger.LogInformation("Deleting saved query ID={Id}", id);
                return await _dataManagerHelper.DeleteSavedQueryAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavedAINLQService.DeleteAsync for ID={Id}", id);
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
                return await _dataManagerHelper.GetSavedQueryByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavedAINLQService.GetByIdAsync for ID={Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Get saved queries with pagination and filtering
        /// </summary>
        public async Task<(List<SavedAINLQ> Queries, int TotalCount)> GetPaginatedAsync(
            string userId, int? applicationId = null, bool? hasData = null, 
            bool? thumbsUp = null, string? searchTerm = null, 
            int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Getting paginated queries for User={UserId}, Page={PageNumber}", 
                    userId, pageNumber);

                return await _dataManagerHelper.GetPaginatedSavedQueriesAsync(
                    userId, applicationId, hasData, thumbsUp, searchTerm, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavedAINLQService.GetPaginatedAsync for User={UserId}", userId);
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
                _logger.LogInformation("Updating feedback for query ID={Id}, ThumbsUp={ThumbsUp}", 
                    id, thumbsUp);

                return await _dataManagerHelper.UpdateFeedbackAsync(id, thumbsUp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavedAINLQService.UpdateFeedbackAsync for ID={Id}", id);
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
                _logger.LogInformation("Updating usage for query ID={Id}", id);
                return await _dataManagerHelper.UpdateUsageAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavedAINLQService.UpdateUsageAsync for ID={Id}", id);
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
                _logger.LogInformation("Getting usage stats for User={UserId}", userId);
                return await _dataManagerHelper.GetUsageStatsAsync(userId, applicationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavedAINLQService.GetUsageStatsAsync for User={UserId}", userId);
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
                _logger.LogInformation("Getting sample queries, Limit={Limit}", limit);
                return await _dataManagerHelper.GetSampleQueriesAsync(applicationId, category, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavedAINLQService.GetSampleQueriesAsync");
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
                _logger.LogInformation("Getting popular queries, Limit={Limit}", limit);
                return await _dataManagerHelper.GetPopularQueriesAsync(applicationId, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavedAINLQService.GetPopularQueriesAsync");
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
                _logger.LogInformation("Inserting sample query: {Label}", sampleQuery.Label);
                return await _dataManagerHelper.InsertSampleQueryAsync(sampleQuery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavedAINLQService.InsertSampleQueryAsync");
                throw;
            }
        }
    }
}